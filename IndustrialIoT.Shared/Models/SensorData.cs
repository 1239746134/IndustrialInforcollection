using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Shared.Models
{
    /// <summary>
    /// 传感器数据
    /// </summary>
    public class SensorData
    {
        //温度
        public double Temperature { get; set; }

        //湿度
        public double Humidity { get; set; }

        //压力
        public double Pressure { get; set; }

        //采集时间戳
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 从原始保持寄存器解析传感器数据
        /// </summary>
        /// <param name="registers"></param>
        /// <returns></returns>
        public static SensorData FromRegisters(ushort[] registers)
        {
            if (registers.Length < 3)
            {
                throw new ArgumentException("寄存器数据不足，至少需要 3 个保持寄存器");
            }

            return new SensorData
            {
                Temperature = registers[0] / 100.0,
                Humidity = registers[1] / 100.0,
                Pressure = registers[2] / 100.0,
                Timestamp = DateTime.Now
            };
        }
    }
}
