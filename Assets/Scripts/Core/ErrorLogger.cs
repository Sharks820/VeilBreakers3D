using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Centralized logging utility for VeilBreakers.
    /// Provides consistent formatting, conditional compilation, and log levels.
    /// </summary>
    public static class ErrorLogger
    {
        // Log level thresholds
        public enum LogLevel
        {
            Verbose = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5,
            None = 6
        }

        private const string kPrefix = "[VB]";
        private const string kCombatPrefix = "[VB:Combat]";
        private const string kUIPrefix = "[VB:UI]";
        private const string kAudioPrefix = "[VB:Audio]";
        private const string kSavePrefix = "[VB:Save]";
        private const string kAIPrefix = "[VB:AI]";
        private const string kCapturePrefix = "[VB:Capture]";

        // Current minimum log level (can be changed at runtime)
        private static LogLevel _minLevel = LogLevel.Debug;

        /// <summary>
        /// Gets or sets the minimum log level. Messages below this level are ignored.
        /// </summary>
        public static LogLevel MinimumLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        // ============================================================
        // GENERAL LOGGING
        // ============================================================

        /// <summary>
        /// Log a general info message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            if (_minLevel <= LogLevel.Info)
                Debug.Log($"{kPrefix} {message}");
        }

        /// <summary>
        /// Log a general info message with context object.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message, UnityEngine.Object context)
        {
            if (_minLevel <= LogLevel.Info)
                Debug.Log($"{kPrefix} {message}", context);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            if (_minLevel <= LogLevel.Warning)
                Debug.LogWarning($"{kPrefix} {message}");
        }

        /// <summary>
        /// Log a warning message with context object.
        /// </summary>
        public static void Warn(string message, UnityEngine.Object context)
        {
            if (_minLevel <= LogLevel.Warning)
                Debug.LogWarning($"{kPrefix} {message}", context);
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string message)
        {
            if (_minLevel <= LogLevel.Error)
                Debug.LogError($"{kPrefix} {message}");
        }

        /// <summary>
        /// Log an error message with context object.
        /// </summary>
        public static void Error(string message, UnityEngine.Object context)
        {
            if (_minLevel <= LogLevel.Error)
                Debug.LogError($"{kPrefix} {message}", context);
        }

        /// <summary>
        /// Log an exception with full stack trace.
        /// </summary>
        public static void Exception(Exception ex, string context = null)
        {
            if (_minLevel <= LogLevel.Critical)
            {
                var msg = string.IsNullOrEmpty(context)
                    ? $"{kPrefix} Exception: {ex.Message}"
                    : $"{kPrefix} [{context}] Exception: {ex.Message}";
                Debug.LogError(msg);
                Debug.LogException(ex);
            }
        }

        // ============================================================
        // SUBSYSTEM-SPECIFIC LOGGING
        // ============================================================

        /// <summary>
        /// Log a combat system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Combat(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kCombatPrefix} {message}");
        }

        /// <summary>
        /// Log a UI system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void UI(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kUIPrefix} {message}");
        }

        /// <summary>
        /// Log an audio system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Audio(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kAudioPrefix} {message}");
        }

        /// <summary>
        /// Log a save system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Save(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kSavePrefix} {message}");
        }

        /// <summary>
        /// Log settings-related messages (volume, graphics, controls, etc.)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Settings(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"[VB:Settings] {message}");
        }

        /// <summary>
        /// Log an AI/Gambit system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void AI(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kAIPrefix} {message}");
        }

        /// <summary>
        /// Log a capture system message.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Capture(string message)
        {
            if (_minLevel <= LogLevel.Debug)
                Debug.Log($"{kCapturePrefix} {message}");
        }

        // ============================================================
        // ASSERTIONS
        // ============================================================

        /// <summary>
        /// Assert a condition is true, log error if false.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                Debug.LogError($"{kPrefix} ASSERTION FAILED: {message}");
        }

        /// <summary>
        /// Assert an object is not null, log error if null.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void AssertNotNull(object obj, string objectName)
        {
            if (obj == null)
                Debug.LogError($"{kPrefix} ASSERTION FAILED: {objectName} is null");
        }

        // ============================================================
        // PERFORMANCE LOGGING
        // ============================================================

        /// <summary>
        /// Log a performance-related message (frame time, allocation warnings, etc.).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Perf(string message)
        {
            if (_minLevel <= LogLevel.Verbose)
                Debug.Log($"{kPrefix}[Perf] {message}");
        }

        /// <summary>
        /// Begin a timed section. Returns a Stopwatch for manual timing.
        /// </summary>
        public static Stopwatch BeginTiming(string sectionName)
        {
            var sw = Stopwatch.StartNew();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_minLevel <= LogLevel.Verbose)
                Debug.Log($"{kPrefix}[Perf] BEGIN: {sectionName}");
            #endif
            return sw;
        }

        /// <summary>
        /// End a timed section and log the duration.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void EndTiming(Stopwatch sw, string sectionName, float warnThresholdMs = 16.67f)
        {
            sw.Stop();
            var ms = sw.Elapsed.TotalMilliseconds;
            var level = ms > warnThresholdMs ? "WARN" : "OK";
            if (_minLevel <= LogLevel.Verbose || ms > warnThresholdMs)
                Debug.Log($"{kPrefix}[Perf] END: {sectionName} - {ms:F2}ms [{level}]");
        }

        // ============================================================
        // EDITOR-ONLY UTILITIES
        // ============================================================

        #if UNITY_EDITOR
        /// <summary>
        /// Clear the Unity console (Editor only).
        /// </summary>
        public static void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method?.Invoke(null, null);
        }
        #endif
    }
}
