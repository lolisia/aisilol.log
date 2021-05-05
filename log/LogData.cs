using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace aisilol.log
{
    public struct LogData<T_SEVERITY>
    {
        public DateTime Created;
        public T_SEVERITY Severity;
        public string Text;

        // ReSharper disable once StaticMemberInGenericType
        private static int _severityLength;

        private static Lazy<Regex> _regex = new(() =>
            new Regex(@"<color\s(?<color>.*?)>(?<content>.*?)<\/color>", RegexOptions.IgnoreCase), true);
        
        public string SeverityHeader
        {
            get
            {
                var text = Severity.ToString();
                if (string.IsNullOrEmpty(text))
                    return "UNKNOWN";

                if (_severityLength == 0)
                {
                    _severityLength = (from T_SEVERITY severity in Enum.GetValues(typeof(T_SEVERITY))
                        select severity.ToString()?.Length ?? 0).Prepend(0).Max();
                }

                var pad = (_severityLength - text.Length) / 2;
                return $"{text.PadLeft(pad + text.Length).PadRight(_severityLength)}";
            }
        }

        public string PlainText
        {
            get
            {
                var builder = new StringBuilder(Text);

                foreach (Match match in _regex.Value.Matches(Text))
                {
                    var content = match.Groups["content"].Value;
                    builder.Replace(match.Value, content);
                }

                return builder.ToString();
            }
        }

        public LogData(T_SEVERITY severity, string text)
        {
            Created = DateTime.Now;
            Severity = severity;
            Text = text;
        }
    }
}