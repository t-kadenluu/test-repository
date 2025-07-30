using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using FluentValidation;
using System.Globalization;

namespace Logging.Services
{
    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IValidator<LogEntry> _validator;
        private readonly string _logDirectory;

        public LoggingService(
            ILogger<LoggingService> logger,
            IConfiguration configuration,
            IMapper mapper,
            IValidator<LogEntry> validator)
        {
            _logger = logger;
            _configuration = configuration;
            _mapper = mapper;
            _validator = validator;
            _logDirectory = _configuration["Logging:Directory"] ?? "logs";
            
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task WriteLogAsync(LogLevel level, string message, string source = null, Exception exception = null)
        {
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Level = level,
                Message = message,
                Source = source,
                Exception = exception?.ToString(),
                Timestamp = DateTime.UtcNow,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            var validationResult = await _validator.ValidateAsync(logEntry);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid log entry: {Errors}", string.Join(", ", validationResult.Errors));
                return;
            }

            await WriteToFileAsync(logEntry);
            _logger.Log(level, "{Source}: {Message}", source, message);
        }

        private async Task WriteToFileAsync(LogEntry entry)
        {
            var fileName = $"app-{DateTime.UtcNow:yyyy-MM-dd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);

            var logLine = FormatLogEntry(entry);
            
            await File.AppendAllTextAsync(filePath, logLine + Environment.NewLine);
        }

        private string FormatLogEntry(LogEntry entry)
        {
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.ThreadId}] {entry.Source}: {entry.Message}" +
                   (entry.Exception != null ? $" | Exception: {entry.Exception}" : "");
        }

        public async Task<List<LogEntry>> GetLogsAsync(LogFilter filter = null)
        {
            var logs = new List<LogEntry>();
            
            if (filter?.Date.HasValue == true)
            {
                var fileName = $"app-{filter.Date.Value:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    var lines = await File.ReadAllLinesAsync(filePath);
                    logs.AddRange(ParseLogLines(lines));
                }
            }
            else
            {
                var files = Directory.GetFiles(_logDirectory, "*.log");
                foreach (var file in files)
                {
                    var lines = await File.ReadAllLinesAsync(file);
                    logs.AddRange(ParseLogLines(lines));
                }
            }

            if (filter != null)
            {
                logs = FilterLogs(logs, filter);
            }

            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }

        private List<LogEntry> ParseLogLines(string[] lines)
        {
            var entries = new List<LogEntry>();
            
            foreach (var line in lines)
            {
                try
                {
                    var entry = ParseLogLine(line);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse log line: {Line}", line);
                }
            }

            return entries;
        }

        private LogEntry ParseLogLine(string line)
        {
            // Simple parsing logic - in reality you'd want more robust parsing
            var parts = line.Split("] [");
            if (parts.Length < 4) return null;

            var timestampStr = parts[0].Substring(1); // Remove leading [
            var levelStr = parts[1];
            var threadIdStr = parts[2];
            var rest = string.Join("] [", parts.Skip(3));

            if (DateTime.TryParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss.fff", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp) &&
                Enum.TryParse<LogLevel>(levelStr, out var level) &&
                int.TryParse(threadIdStr, out var threadId))
            {
                var messageStart = rest.IndexOf(": ");
                if (messageStart > 0)
                {
                    var source = rest.Substring(0, messageStart);
                    var message = rest.Substring(messageStart + 2);

                    return new LogEntry
                    {
                        Timestamp = timestamp,
                        Level = level,
                        ThreadId = threadId,
                        Source = source,
                        Message = message
                    };
                }
            }

            return null;
        }

        private List<LogEntry> FilterLogs(List<LogEntry> logs, LogFilter filter)
        {
            var filtered = logs.AsEnumerable();

            if (filter.Level.HasValue)
            {
                filtered = filtered.Where(l => l.Level == filter.Level.Value);
            }

            if (!string.IsNullOrEmpty(filter.Source))
            {
                filtered = filtered.Where(l => l.Source?.Contains(filter.Source, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrEmpty(filter.Message))
            {
                filtered = filtered.Where(l => l.Message?.Contains(filter.Message, StringComparison.OrdinalIgnoreCase) == true);
            }

            return filtered.ToList();
        }
    }

    public class LogEntry
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public int ThreadId { get; set; }
    }

    public class LogFilter
    {
        public DateTime? Date { get; set; }
        public LogLevel? Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }
}
