using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Shared.Models
{
    public class SimulatorConfig
    {
        public SlaveConfig SlaveConfig { get; set; } = new();

        public DataStoreConfig DataStoreConfig { get; set; } = new();
    }


    public class SlaveConfig
    {
        public byte SlaveId { get; set; }
        public string? PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public string? StopBits { get; set; }
        public string? Parity { get; set; }
    }


    public class DataStoreConfig
    {
        public ushort CoilsCount { get; set; }
        public ushort DiscreteInputsCount { get; set; }
        public ushort HoldingRegistersCount { get; set; }
        public ushort InputRegistersCount { get; set; }
    }
}
