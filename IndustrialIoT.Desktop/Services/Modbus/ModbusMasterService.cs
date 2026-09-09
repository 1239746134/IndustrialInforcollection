using IndustrialIoT.Shared.Models;
using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IndustrialIoT.Desktop.Services.Modbus
{
    public class ModbusMasterService : IModbusMasterService
    {
        private SerialPort? _serialPort;
        private IModbusSerialMaster? _master;

        //日志
        private readonly ILogger<ModbusMasterService>? _logger;

        public event EventHandler<string> OnCommunicationError;


        public bool IsConnected {  get; private set; }

        public ModbusMasterService(ILogger<ModbusMasterService> logger = null!)
        {
            _logger = logger;
        }


        public async Task ConnectAsync(SerialPortConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            if (IsConnected == true) throw new InvalidOperationException("已经连接，请先断开再重连");

            try
            {
                //创建串口
                _serialPort = new SerialPort
                {
                    PortName = config.PortName,
                    BaudRate = config.BaudRate,
                    Parity = config.Parity,
                    DataBits = config.DataBits,
                    StopBits = config.StopBits,

                    ReadTimeout = 2000,
                    WriteTimeout = 2000
                };

                _serialPort.Open();

                //创建Modbus RTU 主站
                _master = ModbusSerialMaster.CreateRtu(_serialPort);
                _master.Transport.ReadTimeout = 2000;
                _master.Transport.WriteTimeout = 2000;

                IsConnected = true;

                _logger?.LogInformation($"Modbus 主站已连接串口 {config.PortName}");
            }
            catch (Exception ex)
            {
                // 清理资源
                _serialPort?.Close();
                _serialPort?.Dispose();
                _serialPort = null;
                _master = null;
                IsConnected = false;

                _logger?.LogError(ex, $"连接串口失败: {ex.Message}");

                // 重新抛出异常，让调用方感知连接失败
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (IsConnected==true)
            {
                _serialPort?.Close();
                _serialPort?.Dispose();
                _serialPort = null;
                _master = null;
                IsConnected = false;
                _logger?.LogInformation("断开连接，释放资源");
            }
            await Task.CompletedTask;
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, 
            ushort count)
        {
            if (IsConnected == false) throw new InvalidOperationException("Modbus 主站未连接");
            if (_master == null) throw new InvalidOperationException("Modbus 主站对象无效");

            try
            {
                return await _master.ReadHoldingRegistersAsync(slaveId, startAddress, count); 
            }
            catch(Exception ex)
            {
                OnCommunicationError?.Invoke(this, $"读取寄存器失败: {ex.Message}");
                _logger?.LogInformation(ex, "读取保持寄存器失败");
                
                return Array.Empty<ushort>();
            }
        }
    }
}
