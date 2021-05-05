using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace aisilol.log
{
    public abstract class PublisherBase<T_SEVERITY> 
        where T_SEVERITY : Enum
    {
        public TimeSpan PublishDelay { get; set; } = TimeSpan.FromMilliseconds(10);

        public abstract T_SEVERITY UseSeverity { get; set; } 
        
        private readonly List<LogData<T_SEVERITY>> _logs = new();
        private readonly SemaphoreLock _logLock = new();

        private readonly CancellationTokenSource _tokenSource = new();
        private Task _publisher;

        private readonly List<ISubscriber<T_SEVERITY>> _subscribers = new();
        private readonly SemaphoreLock _subscriberLock = new();
        
        private async Task ProcPublish(CancellationToken token)
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(PublishDelay, token);

                    using (await _subscriberLock.LockAsync())
                    using (await _logLock.LockAsync())
                    {
                        foreach (var subscriber in _subscribers)
                        {
                            await subscriber.WriteAsync(_logs, token);
                        }
                    
                        _logs.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void AddSubscriber(ISubscriber<T_SEVERITY> subscriber)
        {
            using (_subscriberLock.Lock())
            {
                _subscribers.Add(subscriber);
            }
        }

        public void RemoveSubscriber(ISubscriber<T_SEVERITY> subscriber)
        {
            using (_subscriberLock.Lock())
            {
                _subscribers.RemoveAll(s => s == subscriber);
            }
        }
        
        public void Write(T_SEVERITY severity, string text)
        {
            if (!UseSeverity.HasFlag(severity))
                return;
            
            using (_logLock.Lock())
            {
                _logs.Add(new LogData<T_SEVERITY>(severity, text));
            }
        }

        public void Write(T_SEVERITY severity, Exception e) => Write(severity, e.ToString());

        public void Run()
        {
            _publisher = ProcPublish(_tokenSource.Token);
        }

        public void Stop()
        {
            _tokenSource.Cancel();
            _publisher.Wait();
        }
    }

    public class LOG : PublisherBase<LogSeverity>
    {
        public override LogSeverity UseSeverity { get; set; } = LogSeverity.Debug | LogSeverity.Info | LogSeverity.Warning |
                                                           LogSeverity.Error | LogSeverity.Exception |
                                                           LogSeverity.Fatal;

        public static LOG Instance { get; } = new();

        public static void Debug(string text) => Instance.Write(LogSeverity.Debug, text);
        public static void Info(string text) => Instance.Write(LogSeverity.Info, text);
        public static void Warning(string text) => Instance.Write(LogSeverity.Warning, text);
        public static void Error(string text) => Instance.Write(LogSeverity.Error, text);
        public static void Exception(string text) => Instance.Write(LogSeverity.Exception, text);
        public static void Fatal(string text) => Instance.Write(LogSeverity.Fatal, text);
        
        public static void Exception(Exception e) => Instance.Write(LogSeverity.Exception, e);
    }
}