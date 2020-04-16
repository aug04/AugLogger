using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Aug
{
    /// <summary>
    /// This class gives you an easy way to write your log in C#.
    /// </summary>
    public class Logger
    {
        private static readonly string _defaultTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";
        private readonly string _type;
        private readonly string _filePath;
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private string _timeFormat;
        private readonly string _directory;

        private Logger(string type, string timeFormat, string filePath, string directory)
        {
            _type = type;
            _timeFormat = !string.IsNullOrEmpty(timeFormat) ? timeFormat : _defaultTimeFormat;
            _filePath = filePath;
            _directory = directory;
        }

        private static Logger GetLogger(Type type, string timeFormat, string filePath, string directory)
        {
            return new Logger(type.Name, timeFormat, filePath, directory);
        }

        /// <summary>
        /// Create Logger instance by type.
        /// </summary>
        /// <param name="type">Type class (can be null).</param>
        /// <returns></returns>
        public static Logger GetLogger(Type type)
        {
            return new Logger(type?.Name, _defaultTimeFormat, null, null);
        }

        /// <summary>
        /// Set time format for the logger.
        /// </summary>
        /// <param name="timeFormat">DateTime format string in C#, default value is "yyyy-MM-dd HH:mm:ss,fff".</param>
        /// <returns>The logger instance.</returns>
        public Logger SetTimeFormat(string timeFormat)
        {
            if (!string.IsNullOrEmpty(timeFormat)) _timeFormat = timeFormat;
            return this;
        }

        /// <summary>
        /// Log the message with information type (INFO tag).
        /// </summary>
        /// <param name="message">The text to log to file & console.</param>
        public void Info(string message)
        {
            WriteLog(message, LogType.Info);
        }

        /// <summary>
        /// Log the message with information type (DEBUG tag).
        /// </summary>
        /// <param name="message">The text to log to file & console.</param>
        public void Debug(string message)
        {
            WriteLog(message, LogType.Debug);
        }

        /// <summary>
        /// Log the message with information type (WARNING tag).
        /// </summary>
        /// <param name="message">The text to log to file & console.</param>
        public void Warning(string message)
        {
            WriteLog(message, LogType.Warning);
        }

        /// <summary>
        /// Log the message with information type (ERROR tag).
        /// </summary>
        /// <param name="message">The text to log to file & console.</param>
        public void Error(string message)
        {
            WriteLog(message, LogType.Error);
        }

        /// <summary>
        /// Log the message with information type (VERBOSE tag).
        /// </summary>
        /// <param name="message">The text to log to file & console.</param>
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
                        !string.IsNullOrEmpty(_directory) ? _directory : Assembly.GetExecutingAssembly().GetName().Name, "Log");
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

        /// <summary>
        /// This class is used to build a logger instance by chain.
        /// </summary>
        public class Builder
        {
            private string _filePath;
            private string _timeFormat;
            private string _directory;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            private Type _type;

            /// <summary>
            /// Create Builder instance.
            /// </summary>
            public Builder()
            {
                _timeFormat = _defaultTimeFormat;
                _filePath = null;
                _directory = null;
            }

            /// <summary>
            /// Create Builder instance by type.
            /// </summary>
            /// <param name="type">Type class.</param>
            public Builder(Type type)
            {
                _type = type;
                _timeFormat = _defaultTimeFormat;
                _filePath = null;
                _directory = null;
            }

            /// <summary>
            /// Set time format for the logger.
            /// </summary>
            /// <param name="timeFormat">DateTime format string in C#, default value is "yyyy-MM-dd HH:mm:ss,fff".</param>
            /// <returns>The Builder instance allow to chain.</returns>
            public Builder SetTimeFormat(string timeFormat)
            {
                if (!string.IsNullOrEmpty(timeFormat)) _timeFormat = timeFormat;
                return this;
            }

            /// <summary>
            /// The type name is used to filter.
            /// </summary>
            /// <param name="type">Any type, normally is class type, example: typeof(ClassName).</param>
            /// <returns>The Builder instance allow to chain.</returns>
            public Builder SetType(Type type)
            {
                _type = type;
                return this;
            }

            /// <summary>
            /// Set your absolute log file path.
            /// </summary>
            /// <param name="filePath">The absolute file path to stored all logged message.</param>
            /// <returns>The Builder instance allow to chain.</returns>
            public Builder SetFilePath(string filePath)
            {
                _filePath = filePath;
                return this;
            }

            /// <summary>
            /// Set your log directory name.
            /// </summary>
            /// <param name="directoryName">The directory name to stored all logged message (normally is project name).</param>
            /// <returns>The Builder instance allow to chain.</returns>
            public Builder SetDirectoryName(string directoryName)
            {
                _directory = directoryName;
                return this;
            }

            /// <summary>
            /// This method build an instance of the Logger.
            /// </summary>
            /// <returns>The instance of the Logger</returns>
            public Logger Build()
            {
                return GetLogger(_type, _timeFormat, _filePath, _directory);
            }
        }
    }
}