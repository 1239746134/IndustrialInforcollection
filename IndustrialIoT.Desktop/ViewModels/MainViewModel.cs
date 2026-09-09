using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialIoT.Desktop.Services.Modbus;
using IndustrialIoT.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Timers;
using System.Windows;

namespace IndustrialIoT.Desktop.ViewModels
{
     public partial class MainViewModel : ObservableObject
    {
        private readonly IModbusMasterService _service;
        private System.Timers.Timer? _autoReadTimer;

        //串口配置属性
        [ObservableProperty]
        private SerialPortConfig _portConfig = new SerialPortConfig()
        {
            PortName = "COM3",
            BaudRate = 9600,
            DataBits = 8,
            StopBits = System.IO.Ports.StopBits.One,
            Parity = System.IO.Ports.Parity.None,
            SlaveId = 1,
            StartAddress = 0,
            ReadCount = 3
        };



        //命令
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
        [NotifyCanExecuteChangedFor(nameof(SingleReadCommand))]
        [NotifyCanExecuteChangedFor(nameof(AutoReadCommand))]
        private bool _isConnected;


        [ObservableProperty]
        private bool _isAutoReading;

        [ObservableProperty]
        private string _lastReadTime = "--:--:--";

        [ObservableProperty]
        private int _readCountTotal;

        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


        public ObservableCollection<CommunicationLogItem> CommunicationLogs { get; } = new();


        //下拉选项列表
        public ObservableCollection<string> PortNames { get; } = new();
        public List<int> BaudRateOptions { get; } = new() { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        public List<int> DataBitsOptions { get; } = new() { 7, 8 };
        public List<string> StopBitsOptions { get; } = new() { "One", "Two" };
        public List<string> ParityOptions { get; } = new() { "None", "Odd", "Even"};
        

        //数据卡片
        public DataCardViewModel DataCard { get; } = new();

        //趋势图
        public TrendChartViewModel TrendChart { get; } = new();


        public MainViewModel(IModbusMasterService service)
        {
            this._service = service;
            _service.OnCommunicationError += OnCommError;

            DispatcherTimer clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clockTimer.Start();

            string[] ports = SerialPort.GetPortNames();
            foreach(string port in ports)
            {
                PortNames.Add(port);
            }
        }


        //四个命令:
        //1. ConnectCommand（连接）
        private bool CanConnect() => !IsConnected;
        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            AddLog("正在连接...");
            try
            {
                await _service.ConnectAsync(PortConfig);
                IsConnected = _service.IsConnected;
                AddLog("连接成功");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                AddLog($"连接失败: {ex.Message}", "Error");
            }
        }


        //2. DisconnectCommand（断开）
        private bool CanDisconnect() => IsConnected;
        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private async Task DisconnectAsync()
        {
            if (IsAutoReading) ToggleAutoRead(); // 先停自动读取
            await _service.DisconnectAsync();
            IsConnected = false;
            AddLog("已断开连接");
        }

        //3. SingleReadCommand(单次读取)
        private bool CanRead() => IsConnected;
        [RelayCommand(CanExecute = nameof(CanRead))]
        private async Task SingleReadAsync()
        {
            await DoReadAsync();
        }

        //4. AutoReadCommand(自动读取）

        [RelayCommand(CanExecute = nameof(CanRead))]
        private void AutoRead()
        {
            ToggleAutoRead();
        }

        private void ToggleAutoRead()
        {
            if (IsAutoReading)
            {
                _autoReadTimer?.Stop();
                _autoReadTimer?.Dispose();
                _autoReadTimer = null;
                IsAutoReading = false;
                AddLog("停止自动读取");
            }
            else
            {
                _autoReadTimer = new System.Timers.Timer(2000); // 每 2 秒读一次
                _autoReadTimer.Elapsed += async (_, _) => await Application.Current.Dispatcher.InvokeAsync(DoReadAsync);
                _autoReadTimer.AutoReset = true;
                _autoReadTimer.Start();
                IsAutoReading = true;
                AddLog("开始自动读取");
            }
        }



        private async Task DoReadAsync()
        {
            try
            {
                ushort[] raw = await _service.ReadHoldingRegistersAsync(PortConfig.SlaveId, PortConfig.StartAddress, PortConfig.ReadCount);
                if (raw.Length >= 3)
                {
                    SensorData data = SensorData.FromRegisters(raw);
                    DataCard.Update(data);
                    TrendChart.AppendData(data);
                    LastReadTime = data.Timestamp.ToString("HH:mm:ss");
                    ReadCountTotal++;
                    AddLog($"读取成功: 温度={data.Temperature:F1}°C, 湿度={data.Humidity:F1}%RH, 压力={data.Pressure:F1}hPa");
                }
            }
            catch (Exception ex)
            {
                AddLog($"读取失败: {ex.Message}", "Error");
            }
        }


        private void AddLog(string msg, string level = "Info")
        {
            CommunicationLogs.Insert(0, new CommunicationLogItem
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = msg
            });
            // 限制日志数量
            while (CommunicationLogs.Count > 50)
                CommunicationLogs.RemoveAt(CommunicationLogs.Count - 1);
        }

        private void OnCommError(object? sender, string msg)
        {
            Application.Current.Dispatcher.Invoke(() => AddLog(msg, "Error"));
        }
    }
}
