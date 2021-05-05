using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace aisilol.log
{
    public interface ISubscriber<T_SEVERITY>
        where T_SEVERITY : Enum
    {
        Task WriteAsync(IEnumerable<LogData<T_SEVERITY>> data, CancellationToken token);
    }
    
    public class DefaultConsoleLog : ISubscriber<LogSeverity>
    {
        private static readonly Lazy<Regex> _regexContent = new(() =>
            new Regex(@"<color\s(?<color>.*?)>(?<content>.*?)<\/color>", RegexOptions.IgnoreCase), true);
        private static readonly Lazy<Regex> _regexColor = new(() =>
            new Regex(@"(?<type>.\w)=(?<color>.\w+)", RegexOptions.IgnoreCase), true);

        private readonly StringBuilder _builder = new();

        private readonly Dictionary<LogSeverity, (ConsoleColor, ConsoleColor)> _colorMap = new();
        private readonly ConsoleColor _defaultForegroundColor = Console.ForegroundColor;
        private readonly ConsoleColor _defaultBackgroundColor = Console.BackgroundColor;

        public DefaultConsoleLog()
        {
            _colorMap.Add(LogSeverity.Debug, (ConsoleColor.Green, ConsoleColor.Black));
            _colorMap.Add(LogSeverity.Info, (ConsoleColor.White, ConsoleColor.Black));
            _colorMap.Add(LogSeverity.Warning, (ConsoleColor.Yellow, ConsoleColor.Black));
            _colorMap.Add(LogSeverity.Error, (ConsoleColor.Red, ConsoleColor.Black));
            _colorMap.Add(LogSeverity.Exception, (ConsoleColor.Magenta, ConsoleColor.Black));
            _colorMap.Add(LogSeverity.Fatal, (ConsoleColor.DarkRed, ConsoleColor.DarkYellow));
        }

        private void WriteDefault(string text)
        {
            Console.ForegroundColor = _defaultForegroundColor;
            Console.BackgroundColor = _defaultBackgroundColor;
            
            Console.Write(text);
        }

        private void Write(string text, ConsoleColor foreground, ConsoleColor background)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            
            Console.Write(text);

            Console.ForegroundColor = _defaultForegroundColor;
            Console.BackgroundColor = _defaultBackgroundColor;
        }
        
        public Task WriteAsync(IEnumerable<LogData<LogSeverity>> data, CancellationToken token)
        {
            foreach (var d in data)
            {
                var (foreground, background) = _colorMap[d.Severity];
                var text = $"[{d.Created:HH:mm:ss}][<color fg={foreground} bg={background}>{d.SeverityHeader}</color>] {d.Text}";

                var begin = text.IndexOf("<color ", StringComparison.Ordinal);
                WriteDefault(text[..begin]);

                text = text.Remove(0, begin);
                
                while (true)
                {
                    var match = _regexContent.Value.Match(text);
                    if (match.Length == 0)
                    {
                        Console.WriteLine(text);
                        break;
                    }

                    WriteDefault(text[..match.Index]);
                    text = text.Remove(0, match.Index);
                    
                    var foregroundColor = _defaultForegroundColor;
                    var backgroundColor = _defaultBackgroundColor;
                    var colors = match.Groups["color"].Value;
                    
                    foreach (Match m in _regexColor.Value.Matches(colors))
                    {
                        switch (m.Groups["type"].Value)
                        {
                            case "fg": 
                                Enum.TryParse(m.Groups["color"].Value, out foregroundColor);
                                break;
                            case "bg":
                                Enum.TryParse(m.Groups["color"].Value, out backgroundColor);
                                break;
                        }
                    }
                    
                    Write(match.Groups["content"].Value, foregroundColor, backgroundColor);

                    text = text.Remove(0, match.Length);
                }
            }
            
            return Task.CompletedTask;
        }
    }
    
    public class DefaultFileLog : ISubscriber<LogSeverity>
    {
        protected virtual string FilePath { get; } = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

        private readonly StringBuilder _builder = new();
        
        public async Task WriteAsync(IEnumerable<LogData<LogSeverity>> data, CancellationToken token)
        {
            _builder.Clear();

            foreach (var d in data)
            {
                _builder.AppendLine($"[{d.Created:HH:mm:ss}][{d.SeverityHeader}] {d.PlainText}");
            }

            await File.AppendAllTextAsync(FilePath, _builder.ToString(), token);
        }
    }
}