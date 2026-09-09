using IndustrialIoT.DeviceSimulator.Simulators;

namespace IndustrialIoT.DeviceSimulator;

internal class Program
{
    static void Main(string[] args)
    {
        RtuSlaveSimulator simulator = new RtuSlaveSimulator();
        ManualResetEventSlim mre = new ManualResetEventSlim();
        simulator.Start();

        Console.WriteLine("按 Ctrl+C 停止...");
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("正在清理资源...");
            simulator.Stop();
            mre.Set();
        };

        mre.Wait();
        Thread.Sleep(1000);
        Console.WriteLine("程序即将退出...");
    }
}
