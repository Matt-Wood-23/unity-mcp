using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    [InitializeOnLoad]
    public static class ConsoleDataProvider
    {
        private static readonly List<LogEntry> _logs = new List<LogEntry>();
        private static readonly object _lock = new object();
        private const int MaxLogs = 100;

        static ConsoleDataProvider()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                _logs.Add(new LogEntry
                {
                    Message = condition,
                    StackTrace = stackTrace,
                    Type = type.ToString(),
                    Timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff")
                });

                if (_logs.Count > MaxLogs)
                {
                    _logs.RemoveAt(0);
                }
            }
        }

        public static string GetConsoleLogs()
        {
            List<LogEntry> logsCopy;
            lock (_lock)
            {
                logsCopy = new List<LogEntry>(_logs);
            }

            var data = new ConsoleData
            {
                TotalCount = logsCopy.Count,
                Logs = logsCopy
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
    }
}
