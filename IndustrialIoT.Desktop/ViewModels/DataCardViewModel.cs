using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialIoT.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialIoT.Desktop.ViewModels
{
    public partial class DataCardViewModel : ObservableObject
    {
        // ── 数据 ──
        [ObservableProperty] private double _temperature;
        [ObservableProperty] private double _humidity;
        [ObservableProperty] private double _pressure;

        // ── 状态文本 ──
        [ObservableProperty] private string _temperatureStatus = "--";
        [ObservableProperty] private string _humidityStatus = "--";
        [ObservableProperty] private string _pressureStatus = "--";

        // ── 进度条值 (0-100) ──
        [ObservableProperty] private double _temperatureProgress;
        [ObservableProperty] private double _humidityProgress;
        [ObservableProperty] private double _pressureProgress;

        // ── 状态颜色 ──
        [ObservableProperty] private string _temperatureColor = "#27AE60";
        [ObservableProperty] private string _humidityColor = "#27AE60";
        [ObservableProperty] private string _pressureColor = "#27AE60";


        // 量程配置
        private const double TempMin = -20, TempMax = 80, TempWarn = 55, TempAlarm = 65;
        private const double HumMin = 0, HumMax = 100, HumWarn = 75, HumAlarm = 85;
        private const double PressMin = 0, PressMax = 200, PressWarnHigh = 120, PressAlarmHigh = 150;

        public void Update(SensorData data)
        {
            Temperature = data.Temperature;
            Humidity = data.Humidity;
            Pressure = data.Pressure;

            UpdateMetric(data.Temperature, TempMin, TempMax, TempWarn, TempAlarm,
                out var tStatus, out var tProgress, out var tColor);
            TemperatureStatus = tStatus;
            TemperatureProgress = tProgress;
            TemperatureColor = tColor;

            UpdateMetric(data.Humidity, HumMin, HumMax, HumWarn, HumAlarm,
                out var hStatus, out var hProgress, out var hColor);
            HumidityStatus = hStatus;
            HumidityProgress = hProgress;
            HumidityColor = hColor;

            UpdateMetric(data.Pressure, PressMin, PressMax, PressWarnHigh, PressAlarmHigh,
                out var pStatus, out var pProgress, out var pColor);
            PressureStatus = pStatus;
            PressureProgress = pProgress;
            PressureColor = pColor;
        }

        private static void UpdateMetric(double value, double min, double max,
        double warn, double alarm, out string status, out double progress, out string color)
        {
            progress = Math.Clamp((value - min) / (max - min) * 100, 0, 100);

            if (value >= alarm)
            {
                status = "● 异常"; color = "#E74C3C";
            }
            else if (value >= warn)
            {
                status = "● 警告"; color = "#F39C12";
            }
            else
            {
                status = "● 正常"; color = "#27AE60";
            }
        }
    }
}
