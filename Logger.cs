using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace AugLogger
{
    /// <summary>
    /// This class gives you an easy way to write your log in C#.
    /// </summary>
    public class Logger
    {
        private static readonly string _defaultTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";
        private static string _type;
        private readonly string _filePath;
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private string _timeFormat;

        private Logger(string type, string timeFormat, string filePath)
        {
            _type = type;
            _timeFormat = !string.IsNullOrEmpty(timeFormat) ? timeFormat : _defaultTimeFormat;
            _filePath = filePath;
        }

        private static Logger GetLogger(Type type, string timeFormat, string filePath)
        {
            return new Logger(type.Name, timeFormat, filePath);
        }

        public static Logger GetLogger(Type type)
        {
            return new Logger(type?.Name, _defaultTimeFormat, null);
        }

        public Logger SetTimeFormat(string timeFormat)
        {
            if (!string.IsNullOrEmpty(timeFormat)) _timeFormat = timeFormat;
            return this;
        }

        public void Info(string message)
        {
            WriteLog(message, LogType.Info);
        }

        public void Debug(string message)
        {
            WriteLog(message, LogType.Debug);
        }

        public void Warning(string message)
        {
            WriteLog(message, LogType.Warning);
        }

        public void Error(string message)
        {
            WriteLog(message, LogType.Error);
        }

        public void Verbose(string message)
        {
            WriteLog(message, LogType.Verbose);
        }

        private static string GetLogFileName()
        {
            return DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        }

        private string GetLogType()
        {
            return !string.IsNullOrEmpty(_type) ? _type + " - " : "";
        }

        private string RenderLog(string text, LogType type)
        {
            string prefix;
            var time = DateTime.Now.ToString(_timeFormat);
            switch (type)
            {
                case LogType.Info:
                    prefix = time + " - " + GetLogType() + "INFO";
                    break;
                case LogType.Debug:
                    prefix = time + " - " + GetLogType() + "DEBUG";
                    break;
                case LogType.Warning:
                    prefix = time + " - " + GetLogType() + "WARNING";
                    break;
                case LogType.Error:
                    prefix = time + " - " + GetLogType() + "ERROR";
                    break;
                case LogType.Verbose:
                    prefix = time + " - " + GetLogType() + "VERBOSE";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var message = prefix + " - " + text;
            System.Diagnostics.Debug.WriteLine(message);
            return message;
        }

        private void WriteLog(string text, LogType type)
        {
            _locker.EnterWriteLock();
            try
            {
                var path = _filePath;
                if (!string.IsNullOrEmpty(path))
                {
                    if (!IsFile(path))
                        throw new Exception("Invalid file path! please check your file not a directory.");
                }
                else
                {
                    var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name, "Log");
                    Directory.CreateDirectory(directory);
                    path = Path.Combine(directory, GetLogFileName());
                }

                // Append text to the file
                using (var sw = File.AppendText(path))
                {
                    sw.WriteLine(RenderLog(text, type));
                    sw.Close();
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        private bool IsFile(string filePath)
        {
            var attr = File.GetAttributes(filePath);
            return !attr.HasFlag(FileAttributes.Directory);
        }

        private enum LogType
        {
            Info,
            Debug,
            Warning,
            Error,
            Verbose
        }

        public class Builder
        {
            private string _filePath;
            private string _timeFormat;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            private Type _type;

            public Builder()
            {
                _timeFormat = _defaultTimeFormat;
                _filePath = null;
            }

            public Builder(Type type)
            {
                _type = type;
                _timeFormat = _defaultTimeFormat;
                _filePath = null;
            }

            public Builder SetTimeFormat(string timeFormat)
            {
                if (!string.IsNullOrEmpty(timeFormat)) _timeFormat = timeFormat;
                return this;
            }

            public Builder SetType(Type type)
            {
                _type = type;
                return this;
            }

            public Builder SetFilePath(string filePath)
            {
                _filePath = filePath;
                return this;
            }

            public Logger Build()
            {
                return GetLogger(_type, _timeFormat, _filePath);
            }
        }
    }
}