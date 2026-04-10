using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityMCPBridge.Models;

namespace UnityMCPBridge.Providers
{
    [InitializeOnLoad]
    public static class ConsoleDataProvider
    {
        private static readonly List<LogEntry> _logs = new List<LogEntry>();
        private static readonly object _lock = new object();
        private const int MaxLogs = 500;

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
                    Timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
                });

                if (_logs.Count > MaxLogs)
                    _logs.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns console logs, optionally filtered by type, text search, and max count.
        /// </summary>
        public static string GetConsoleLogs(string typeFilter = null, string search = null, int maxCount = 100)
        {
            List<LogEntry> logsCopy;
            lock (_lock)
            {
                logsCopy = new List<LogEntry>(_logs);
            }

            var filtered = logsCopy.AsEnumerable();

            // Filter by log type (Error, Warning, Log, Exception, Assert)
            if (!string.IsNullOrEmpty(typeFilter))
            {
                var types = typeFilter.Split(',')
                    .Select(t => t.Trim().ToLower())
                    .ToHashSet();

                filtered = filtered.Where(l => types.Contains(l.Type.ToLower()));
            }

            // Filter by text search
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(l =>
                    l.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (l.StackTrace != null && l.StackTrace.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var filteredList = filtered.ToList();
            int filteredCount = filteredList.Count;

            // Take most recent N entries
            if (maxCount > 0 && filteredList.Count > maxCount)
                filteredList = filteredList.Skip(filteredList.Count - maxCount).ToList();

            var data = new FilteredConsoleData
            {
                TotalCount = logsCopy.Count,
                FilteredCount = filteredCount,
                TypeFilter = typeFilter,
                SearchFilter = search,
                Logs = filteredList
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static string ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }

            // Also clear Unity's own console window
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            logEntries?.GetMethod("Clear")?.Invoke(null, null);

            return JsonConvert.SerializeObject(new { success = true, message = "Console cleared" });
        }
    }
}
