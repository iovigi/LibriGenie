using Microsoft.Extensions.Logging;
using System.Text;

namespace LibriGenie.Workers.Services
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath, categoryName, _lock);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly string _categoryName;
        private readonly object _lock;

        public FileLogger(string filePath, string categoryName, object lockObj)
        {
            _filePath = filePath;
            _categoryName = categoryName;
            _lock = lockObj;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}";
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_filePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // If we can't write to the file, we'll just continue without logging
                }
            }
        }
    }
} 