using System;
using System.Collections.Generic;

namespace MagnusAkselvoll.AndroidPhotoBooth.Camera.Logging
{
    public static class Logger
    {
        public const int BufferLength = 200;
        private static readonly LinkedList<LogMessage> LastMessagesList = new LinkedList<LogMessage>();

        public static IReadOnlyCollection<LogMessage> LastMessages => LastMessagesList;

        public static void Log(LogMessageLevel level, string message, TimeSpan? duration = null)
        {
            Log(new LogMessage(level, message, duration));
        }

        public static void Log(LogMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            LastMessagesList.AddFirst(message);

            if (LastMessagesList.Count > BufferLength)
            {
                LastMessagesList.RemoveLast();
            }

            MessageLogged?.Invoke(null, message);
        }

        public static event EventHandler<LogMessage> MessageLogged;
    }
}
