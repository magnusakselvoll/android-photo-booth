using System;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera.Logging
{
    public sealed class LogMessage
    {
        public LogMessage(LogMessageLevel level, string message, TimeSpan? duration)
        {
            Timestamp = DateTime.UtcNow;
            Level = level;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Duration = duration;
        }

        public DateTime Timestamp { get; }
        public DateTime TimestampLocal => TimeZone.CurrentTimeZone.ToLocalTime(Timestamp);
        public LogMessageLevel Level { get; }
        public string Message { get; }
        public TimeSpan? Duration { get; }
    }
}