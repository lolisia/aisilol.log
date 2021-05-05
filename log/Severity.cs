using System;

namespace aisilol.log
{
    [Flags]
    public enum LogSeverity
    {
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        Exception = 16,
        Fatal = 32,
    }
}