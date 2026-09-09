using IndustrialIoT.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Desktop.Services.Modbus
{
    /// <summary>
    /// Modbus RTU 主站服务接口
    /// </summary>
    public interface IModbusMasterService
    {
        bool IsConnected {  get; }

        /// <summary>
        /// 连接串口并创建 Modbus 主站
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task ConnectAsync(SerialPortConfig config);


        /// <summary>
        /// 断开串口连接，释放资源
        /// </summary>
        /// <returns></returns>
        Task DisconnectAsync();

        /// <summary>
        /// 读取保持寄存器
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="count">读取数量</param>
        /// <returns></returns>
        Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort count);


        /// <summary>
        /// 通信错误事件（用于UI提示）
        /// </summary>
        event EventHandler<string> OnCommunicationError;
    }
}
