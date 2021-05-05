using System;
using System.Threading;
using System.Threading.Tasks;
using aisilol.log;

namespace console_test
{
    class Program
    {
        private static async Task Run()
        {
            while (true)
            {
                foreach (LogSeverity e in Enum.GetValues(typeof(LogSeverity)))
                {
                    LOG.Instance.Write(e, $"<color fg=Red>{e.ToString()}</color> / ThreadID : {Thread.CurrentThread.ManagedThreadId} / <color bg=Blue fg=Yellow>{Guid.NewGuid().ToString()}</color>");
                    await Task.Yield();
                }
            }
        }
        
        static void Main(string[] args)
        {
            LOG.Instance.AddSubscriber(new DefaultConsoleLog());
            LOG.Instance.AddSubscriber(new DefaultFileLog());
                
            LOG.Instance.Run();

            _ = Run();
            _ = Run();
            _ = Run();
            
            Console.ReadKey();
            LOG.Instance.Stop();
        }
    }
}