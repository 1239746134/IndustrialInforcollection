using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Shared.Models
{
    /// <summary>
    /// 串口配置
    /// </summary>
    public partial class SerialPortConfig
    {
        public string PortName { get; set; }

        public int BaudRate { get; set; }

        public int DataBits { get; set; }

        public StopBits StopBits { get; set; }

        public Parity Parity { get; set; }

        public byte SlaveId { get; set; }

        public ushort StartAddress { get; set; }

        public ushort ReadCount { get; set; }
    }
}
