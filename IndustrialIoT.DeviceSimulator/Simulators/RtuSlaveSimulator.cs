using IndustrialIoT.Shared.Models;
using Modbus.Data;
using Modbus.Device;
using System;
using System.IO.Ports;
using System.Text.Json;
using System.Timers;

namespace IndustrialIoT.DeviceSimulator.Simulators;

/// <summary>
/// Modbus RTU 从站模拟器
/// </summary>
public class RtuSlaveSimulator
{
    private SerialPort? _serialPort;
    private ModbusSlave? _slave;
    private SimulatorConfig? _config;
    private System.Timers.Timer? _updateTimer;
    private readonly Random _random = new();

    // 当前传感器值
    private double _currentTemp = 30;
    private double _currentHum = 60;
    private double _currentPress = 100;

    public void Start()
    {
        Console.WriteLine("===== Modbus RTU 从站模拟器 =====");

        // 加载配置
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Config", "simulator.config.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("配置文件不存在");
                return;
            }

            string json = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<SimulatorConfig>(json);
            if (_config == null)
            {
                Console.WriteLine("配置文件解析失败");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"读取配置文件出错：{ex.Message}");
            return;
        }

        SlaveConfig slaveConfig = _config.SlaveConfig;
        DataStoreConfig dataStore = _config.DataStoreConfig;

        try
        {
            _serialPort = new SerialPort(slaveConfig.PortName!, slaveConfig.BaudRate,
                ParseParity(slaveConfig.Parity), slaveConfig.DataBits,
                ParseStopBits(slaveConfig.StopBits))
            {
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };


            _slave = ModbusSerialSlave.CreateRtu(slaveConfig.SlaveId, _serialPort);
            _serialPort.Open();

            Console.WriteLine($"串口 {slaveConfig.PortName} 已打开 " +
                $"(波特率:{slaveConfig.BaudRate}, " +
                $"校验位:{ParseParity(slaveConfig.Parity)}, " +
                $"数据位:{slaveConfig.DataBits}, " +
                $"停止位:{ParseStopBits(slaveConfig.StopBits)})");

            // 初始化数据存储区
            _slave.DataStore = DataStoreFactory.CreateDefaultDataStore(
                dataStore.CoilsCount,
                dataStore.DiscreteInputsCount,
                dataStore.HoldingRegistersCount,
                dataStore.InputRegistersCount);

            Console.WriteLine("数据存储区已初始化");

            // 写入初始值
            UpdateRegisters();

            // 启动监听
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("Modbus RTU 从站开始监听...");
                    _slave.ListenAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"监听出错: {ex.Message}");
                }
            });

            // 启动定时更新（每 2 秒随机波动数值）
            _updateTimer = new System.Timers.Timer(2000);
            _updateTimer.Elapsed += OnUpdateTimer;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();

            Console.WriteLine("数值波动模拟已启动（间隔 2 秒）");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动失败: {ex.Message}");
            Stop();
        }
    }

    /// <summary>
    /// 定时随机波动传感器数值
    /// </summary>
    private void OnUpdateTimer(object? sender, ElapsedEventArgs e)
    {
        lock (_random)
        {
            // 温度：每次波动 ±15°C，限制 0~80°C
            _currentTemp += (_random.NextDouble() - 0.5) * 30.0;
            _currentTemp = Math.Round(Math.Clamp(_currentTemp, 0, 80), 2);

            // 湿度：每次波动 ±2.5%RH，限制 0~100%RH
            _currentHum += (_random.NextDouble() - 0.5) * 5.0;
            _currentHum = Math.Round(Math.Clamp(_currentHum, 0, 100), 2);

            // 压力：每次波动 ±1.5 kPa，限制 80~130 kPa
            _currentPress += (_random.NextDouble() - 0.5) * 3.0;
            _currentPress = Math.Round(Math.Clamp(_currentPress, 80, 130), 2);

            UpdateRegisters();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] " +
                $"温度:{_currentTemp:F2}°C  湿度:{_currentHum:F2}%RH  压力:{_currentPress:F2}kPa");
        }
    }

    /// <summary>
    /// 将当前传感器值写入保持寄存器
    /// </summary>
    private void UpdateRegisters()
    {
        if (_slave?.DataStore == null) return;

        _slave.DataStore.HoldingRegisters[1] = (ushort)(_currentTemp * 100);
        _slave.DataStore.HoldingRegisters[2] = (ushort)(_currentHum * 100);
        _slave.DataStore.HoldingRegisters[3] = (ushort)(_currentPress * 100);
    }

    public void Stop()
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        _updateTimer = null;

        if (_slave != null)
        {
            _slave.Dispose();
        }

        if (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                _serialPort.Close();
                _serialPort.Dispose();
                Console.WriteLine("串口已关闭。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭串口时出错: {ex.Message}");
            }
        }
    }

    private static Parity ParseParity(string? parityStr)
    {
        return parityStr?.ToLower() switch
        {
            "none" => Parity.None,
            "odd" => Parity.Odd,
            "even" => Parity.Even,
            "mark" => Parity.Mark,
            "space" => Parity.Space,
            _ => Parity.None
        };
    }

    private static StopBits ParseStopBits(string? stopBitsStr)
    {
        return stopBitsStr?.ToLower() switch
        {
            "one" => StopBits.One,
            "two" => StopBits.Two,
            "onepointfive" => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }
}
