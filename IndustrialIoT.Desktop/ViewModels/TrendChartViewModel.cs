using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialIoT.Shared.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace IndustrialIoT.Desktop.ViewModels;

public partial class TrendChartViewModel : ObservableObject
{
    private const int MaxPoints = 50;

    // 数据点缓冲
    private readonly Queue<DateTimePoint> _tempQueue = new();
    private readonly Queue<DateTimePoint> _humQueue = new();

    // 告警线数据
    private readonly Queue<DateTimePoint> _tempAlarmQueue = new();
    private readonly Queue<DateTimePoint> _humAlarmQueue = new();

    // 系列引用（构造后持有，直接操作 Values）
    private readonly LineSeries<DateTimePoint> _tempSeries;
    private readonly LineSeries<DateTimePoint> _tempAlarmSeries;
    private readonly LineSeries<DateTimePoint> _humSeries;
    private readonly LineSeries<DateTimePoint> _humAlarmSeries;

    // 共用画笔
    private static readonly SolidColorPaint GridPaint = new(SKColor.Parse("#EEEEEE")) { StrokeThickness = 0.5f };
    private static readonly SolidColorPaint AxisLabelPaint = new(SKColor.Parse("#AAAAAA")) { StrokeThickness = 0.5f };
    private static readonly SolidColorPaint TempLinePaint = new(SKColor.Parse("#E74C3C")) { StrokeThickness = 2 };
    private static readonly SolidColorPaint HumLinePaint = new(SKColor.Parse("#3498DB")) { StrokeThickness = 2 };
    private static readonly SolidColorPaint AlarmLinePaint = new(SKColor.Parse("#F39C12")) { StrokeThickness = 1 };
    private static readonly SolidColorPaint MarkerFillPaint = new(SKColors.White);
    private static readonly SolidColorPaint TempMarkerStrokePaint = new(SKColor.Parse("#E74C3C")) { StrokeThickness = 2 };
    private static readonly SolidColorPaint HumMarkerStrokePaint = new(SKColor.Parse("#3498DB")) { StrokeThickness = 2 };

    public ISeries[] TemperatureSeries { get; }
    public ISeries[] HumiditySeries { get; }

    public Axis[] TempXAxes { get; }
    public Axis[] HumXAxes { get; }
    public Axis[] TempYAxes { get; }
    public Axis[] HumYAxes { get; }

    public TrendChartViewModel()
    {
        // ── 温度系列 ──
        _tempSeries = new LineSeries<DateTimePoint>
        {
            Values = Array.Empty<DateTimePoint>(),
            Name = "温度",
            Stroke = TempLinePaint,
            GeometrySize = 7,
            GeometryStroke = TempMarkerStrokePaint,
            GeometryFill = MarkerFillPaint,
            Fill = null,
            LineSmoothness = 0.3
        };
        _tempAlarmSeries = new LineSeries<DateTimePoint>
        {
            Values = Array.Empty<DateTimePoint>(),
            Name = "上限 60°C",
            Stroke = AlarmLinePaint,
            GeometrySize = 0,
            Fill = null
        };
        TemperatureSeries = new ISeries[] { _tempSeries, _tempAlarmSeries };

        // ── 湿度系列 ──
        _humSeries = new LineSeries<DateTimePoint>
        {
            Values = Array.Empty<DateTimePoint>(),
            Name = "湿度",
            Stroke = HumLinePaint,
            GeometrySize = 7,
            GeometryStroke = HumMarkerStrokePaint,
            GeometryFill = MarkerFillPaint,
            Fill = null,
            LineSmoothness = 0.3
        };
        _humAlarmSeries = new LineSeries<DateTimePoint>
        {
            Values = Array.Empty<DateTimePoint>(),
            Name = "上限 80%RH",
            Stroke = AlarmLinePaint,
            GeometrySize = 0,
            Fill = null
        };
        HumiditySeries = new ISeries[] { _humSeries, _humAlarmSeries };

        // ── X 轴 ──
        var xAxes = new Axis
        {
            Labeler = v =>
            {
                var ticks = (long)v;
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                    return "";
                return new DateTime(ticks).ToString("HH:mm:ss");
            },
            LabelsPaint = AxisLabelPaint,
            SeparatorsPaint = GridPaint,
            TextSize = 10
        };
        TempXAxes = new[] { xAxes };
        HumXAxes = new[] { new Axis
        {
            Labeler = xAxes.Labeler,
            LabelsPaint = AxisLabelPaint,
            SeparatorsPaint = GridPaint,
            TextSize = 10
        }};

        // ── Y 轴 ──
        TempYAxes = new[] { new Axis
        {
            MinLimit = -20, MaxLimit = 80, MinStep = 10,
            ForceStepToMin = true, Labeler = v => v + "°C",
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#7F8C8D")),
            SeparatorsPaint = GridPaint, TextSize = 11
        }};
        HumYAxes = new[] { new Axis
        {
            MinLimit = 0, MaxLimit = 100, MinStep = 10,
            ForceStepToMin = true, Labeler = v => v + "%",
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#7F8C8D")),
            SeparatorsPaint = GridPaint, TextSize = 11
        }};
    }

    public void AppendData(SensorData data)
    {
        var now = DateTime.Now;

        // 追加 → 裁剪 → 写入系列 Values
        _tempQueue.Enqueue(new DateTimePoint(now, data.Temperature));
        while (_tempQueue.Count > MaxPoints) _tempQueue.Dequeue();
        _tempSeries.Values = _tempQueue.ToArray();

        _humQueue.Enqueue(new DateTimePoint(now, data.Humidity));
        while (_humQueue.Count > MaxPoints) _humQueue.Dequeue();
        _humSeries.Values = _humQueue.ToArray();

        // 告警线
        UpdateAlarmSeries(_tempAlarmSeries, _tempQueue, 60);
        UpdateAlarmSeries(_humAlarmSeries, _humQueue, 80);
    }

    private static void UpdateAlarmSeries(LineSeries<DateTimePoint> series,
        Queue<DateTimePoint> dataQueue, double y)
    {
        if (dataQueue.Count == 0) return;

        var arr = dataQueue.ToArray();
        series.Values = new[]
        {
            new DateTimePoint(arr[0].DateTime, y),
            new DateTimePoint(arr[^1].DateTime, y)
        };
    }
}
