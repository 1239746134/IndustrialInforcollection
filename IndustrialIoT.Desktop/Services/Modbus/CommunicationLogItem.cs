using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Desktop.Services.Modbus
{
    public class CommunicationLogItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = "";
    }
}
