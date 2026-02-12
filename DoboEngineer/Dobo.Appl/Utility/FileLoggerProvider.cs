
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.LoggingFile {

	public interface  EventId
    {
		public int Id { get; set;}
		public string Name { get; set;}
    }

    public record struct LogMessage(DateTimeOffset TimeStamp, string LogName, TraceLevel Level, ValueTuple<int, string> EventId, string Message, Exception Exception);

    public interface ILoggerProvider : IDisposable
    {
        ILogger CreateLogger(string categoryName);
    }
	public interface ILogger
	{
		public void Write<TState>(TraceLevel logLevel, TState state,
			Exception? exception =null);
      
    }
	public static class LoggerExtensions {
        public static void LogDebug(this ILogger logger, string? message, params object?[] args)
        {
            logger.Write(TraceLevel.Verbose, string.Format(message, args));
        }
        public static void LogInformation(this ILogger logger, string? message, params object?[] args)
        {
            logger.Write(TraceLevel.Info, string.Format(message, args));
        }
        public static void LogWarning(this ILogger logger, string? message, params object?[] args)
        {
            logger.Write(TraceLevel.Warning, string.Format(message, args));
        }
        public static void LogError(this ILogger logger, string? message, params object?[] args)
        {
            logger.Write(TraceLevel.Error, string.Format(message, args));
        }
    }
	public class FileLogger : ILogger
	{

		private readonly string logName;
		private readonly FileLoggerProvider LoggerPrv;

		public FileLogger(string logName, FileLoggerProvider loggerPrv)
		{
			this.logName = logName;
			this.LoggerPrv = loggerPrv; 
		}
		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(TraceLevel logLevel)
		{
			if (logLevel == 0)
				return false;
			return logLevel <= LoggerPrv.MinLevel;
		}

		public void Log<TState>(TraceLevel logLevel, ValueTuple<int,string> eventId, TState state,
			Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			if (formatter == null)
			{
				throw new ArgumentNullException(nameof(formatter));
			}

			string message = formatter(state, exception);

			if (LoggerPrv.FilterLogEntry != null)
				if (!LoggerPrv.FilterLogEntry(new LogMessage(LoggerPrv.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now, logName, logLevel, eventId, message, exception)))
					return;

			if (LoggerPrv.FormatLogEntry != null)
			{
				LoggerPrv.WriteEntry(LoggerPrv.FormatLogEntry(
					new LogMessage(LoggerPrv.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now, logName, logLevel, eventId, message, exception)));
			}
			else
			{
				LoggerPrv.WriteEntry(
					FileLoggerOptions.StringBuilderLogEntryFormat(new LogMessage(LoggerPrv.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now, logName, logLevel, eventId, message, exception))
					);
			}
		}

        public void Write<TState>(TraceLevel logLevel, TState state, Exception exception)
        {
            Log(logLevel,default, state, exception,(p1,p2)=>p1+ ";Exception:" + p2);
        }
    }
    public class FileLoggerOptions
    {
        /// <summary>
        /// Append to existing log files or override them.
        /// </summary>
        public bool Append { get; set; } = true;

        /// <summary>
        /// Determines max size of the one log file.
        /// </summary>
        /// <remarks>If log file limit is specified logger will create new file when limit is reached. 
        /// For example, if log file name is 'test.log', logger will create 'test1.log', 'test2.log' etc.
        /// </remarks>
        public long FileSizeLimitBytes { get; set; } = 0;

        /// <summary>
        /// Determines max number of log files if <see cref="FileSizeLimitBytes"/> is specified.
        /// </summary>
        /// <remarks>If MaxRollingFiles is specified file logger will re-write previously created log files.
        /// For example, if log file name is 'test.log' and max files = 3, logger will use: 'test.log', then 'test1.log', then 'test2.log' and then 'test.log' again (old content is removed).
        /// </remarks>
        public int MaxRollingFiles { get; set; } = 0;

        /// <summary>
        ///  Gets or sets indication whether or not UTC timezone should be used to for timestamps in logging messages. Defaults to false.
        /// </summary>
        public bool UseUtcTimestamp { get; set; }=false;

		/// <summary>
		/// Custom formatter for the log entry line. 
		/// </summary>
		public Func<LogMessage, string> FormatLogEntry { get; set; } = StringBuilderLogEntryFormat;

        public static string StringBuilderLogEntryFormat(LogMessage logMessage)  
		{
			// default formatting logic
			var logBuilder = new StringBuilder();
			if (!string.IsNullOrEmpty(logMessage.Message))
			{
				logBuilder.Append(logMessage.TimeStamp.ToString("o"));
				logBuilder.Append('\t');
				logBuilder.Append(logMessage.Level);
				logBuilder.Append("\t[");
				logBuilder.Append(logMessage.LogName);
				logBuilder.Append("]");
				logBuilder.Append("\t[");
				logBuilder.Append(logMessage.EventId);
				logBuilder.Append("]\t");
				logBuilder.Append(logMessage.Message);
			}

			if (logMessage.Exception != null)
			{
				// exception message
				logBuilder.AppendLine(logMessage.Exception.ToString());
			}
			return logBuilder.ToString();
		}

		///// <summary>
		///// Custom filter for the log entry.
		///// </summary>
		//public Func<LogMessage, bool> FilterLogEntry { get; set; }

        /// <summary>
        /// Minimal logging level for the file logger.
        /// </summary>
        public TraceLevel MinLevel { get; set; } = TraceLevel.Verbose;

        /// <summary>
        /// Custom formatter for the log file name.
        /// </summary>
        /// <remarks>By specifying custom formatting handler you can define your own criteria for creation of log files. Note that this handler is called
        /// on EVERY log message 'write'; you may cache the log file name calculation in your handler to avoid any potential overhead in case of high-load logger usage.
        /// For example:
        /// </remarks>
        /// <example>
        /// fileLoggerOpts.FormatLogFileName = (fname) => {
        ///   return String.Format( Path.GetFileNameWithoutExtension(fname) + "_{0:yyyy}-{0:MM}-{0:dd}" + Path.GetExtension(fname), DateTime.UtcNow); 
        /// };
        /// </example>
        public Func<string, string> FormatLogFileName { get; set; }


        /// <summary>
        /// Determines the naming convention and order of rolling files.
        /// </summary>
        public FileRollingConvention RollingFilesConvention { get; set; } = FileRollingConvention.Ascending;

        /// <summary>
        /// Holds the different file rolling convention, the default option being Ascending.
        /// </summary>
        public enum FileRollingConvention
        {
            /// <summary>
            /// (Default) New files will get an ascending rolling index, files get rolled after max 0-1-2-3-0-1-2-3.
            /// </summary>
            Ascending,
            /// <summary>
            /// New files will get an ascending rolling index, but the latest file is always the file without index. More performant alt for descending rolling. 0-1-2-3-1-2-3
            /// </summary>
            AscendingStableBase,
            /// <summary>
            /// Unix like descending logging, the base will always be stable and contain the latest logs, new files will be incremented and renamed so the highest number is always the oldest. 0-1-2-3
            /// </summary>
            Descending
        }
    }
    /// <summary>
    /// Generic file logger provider.
    /// </summary>
    //[ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider {

		private string LogFileName;

		private readonly ConcurrentDictionary<string, ILogger> loggers =
			new ConcurrentDictionary<string, ILogger>();
		private readonly BlockingCollection<string> entryQueue = new BlockingCollection<string>(1024);
		private readonly Task processQueueTask;
		private readonly FileWriter fWriter;

		internal FileLoggerOptions Options { get; private set; }

		private bool Append => Options.Append;
		private long FileSizeLimitBytes => Options.FileSizeLimitBytes;
		private int MaxRollingFiles => Options.MaxRollingFiles;

		public TraceLevel MinLevel {
			get => Options.MinLevel;
			set { Options.MinLevel = value; }
		}
        /// <summary>
        /// Custom filter for the log entry.
        /// </summary>
        public Func<LogMessage, bool> FilterLogEntry { get; set; }
        /// <summary>
        ///  Gets or sets indication whether or not UTC timezone should be used to for timestamps in logging messages. Defaults to false.
        /// </summary>
        public bool UseUtcTimestamp
		{
			get => Options.UseUtcTimestamp;
			set { Options.UseUtcTimestamp = value; }
		}

		/// <summary>
		/// Custom formatter for log entry. 
		/// </summary>
		public Func<LogMessage, string> FormatLogEntry {
			get => Options.FormatLogEntry;
			set { Options.FormatLogEntry = value; }
		} 

        /// <summary>
        /// Custom formatter for the log file name.
        /// </summary>
        public Func<string, string> FormatLogFileName {
			get => Options.FormatLogFileName;
			set { Options.FormatLogFileName = value; }
		}

		/// <summary>
		/// Custom handler for file errors.
		/// </summary>
		public Func<string,Exception,string> HandleFileError {
			get;set;
		}

		public FileLoggerProvider(string fileName) : this(fileName, true) {
		}

		public FileLoggerProvider(string fileName, bool append) : this(fileName, new FileLoggerOptions() { Append = append }) {
		}

		public FileLoggerProvider(string fileName, FileLoggerOptions options) {
			Options = options;
			LogFileName = Environment.ExpandEnvironmentVariables(fileName);

			fWriter = new FileWriter(this);
			processQueueTask = Task.Factory.StartNew(
				ProcessQueue,
				this,
				TaskCreationOptions.LongRunning);
		}

		public ILogger CreateLogger(string categoryName) {
			return loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
		}

		public void Dispose() {
			entryQueue.CompleteAdding();
			try {
				processQueueTask.Wait(1500);  // the same as in ConsoleLogger
			} catch (TaskCanceledException) { 
			} catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }

			loggers.Clear();
			fWriter.Close();
		}

		protected virtual ILogger CreateLoggerImplementation(string name) {
			return new FileLogger(name, this);
		}

		internal void WriteEntry(string message) {
			if (!entryQueue.IsAddingCompleted) {
				try {
					entryQueue.Add(message);
                    ExtraOutput?.Invoke(message);
                    return;
				} catch (InvalidOperationException) { }
			}
			// do nothing
			ExtraOutput?.Invoke(message);

        }
		private void ProcessQueue() {
			var writeMessageFailed = false;
			foreach (var message in entryQueue.GetConsumingEnumerable()) {
				try {
					if (!writeMessageFailed)
						fWriter.WriteMessage(message, entryQueue.Count == 0);
				} catch (Exception ex) {
					// something goes wrong. App's code can handle it if 'HandleFileError' is provided
					var stopLogging = true;
					if (HandleFileError != null) {
						try {
							var fileErrToNewName=HandleFileError(LogFileName, ex);
							if (fileErrToNewName!=null) {
								fWriter.UseNewLogFile(fileErrToNewName);
								// write failed message to a new log file
								fWriter.WriteMessage(message, entryQueue.Count == 0);
								stopLogging = false;
							}
						} catch {
							// exception is possible in HandleFileError or if proposed file name cannot be used
							// let's ignore it in that case -> file logger will stop processing log messages
						}
					}
					if (stopLogging) {
						// Stop processing log messages since they cannot be written to a log file
						entryQueue.CompleteAdding();
						writeMessageFailed = true;
						UnhandledOutput("writeMessageFailed", ex);
                    }
				}
			}
		}
		public Action<string, Exception> UnhandledOutput { get; set; } = (p, p2) => Console.WriteLine($"{DateTimeOffset.Now}:{p},{p2}");
		public Action<string> ExtraOutput { get; set; }
        private static void ProcessQueue(object state) {
			var fileLogger = (FileLoggerProvider)state;
			fileLogger.ProcessQueue();
		}

		internal class FileWriter {

			readonly FileLoggerProvider FileLogPrv;
			string LogFileName;
			int RollingNumber;
			Stream LogFileStream;
			TextWriter LogFileWriter;

			internal FileWriter(FileLoggerProvider fileLogPrv) {
				FileLogPrv = fileLogPrv;

				DetermineLastFileLogName();
				OpenFile(FileLogPrv.Append);
			}

			string GetBaseLogFileName() {
				var fName = FileLogPrv.LogFileName;
				if (FileLogPrv.FormatLogFileName != null)
					fName = FileLogPrv.FormatLogFileName(fName);
				return fName;
			}

			void DetermineLastFileLogName() {
				var baseLogFileName = GetBaseLogFileName();
				__LastBaseLogFileName = baseLogFileName;
				if (FileLogPrv.FileSizeLimitBytes > 0) {
					// rolling file is used
					if (FileLogPrv.Options.RollingFilesConvention == FileLoggerOptions.FileRollingConvention.Ascending) {
						var logFiles = GetExistingLogFiles(baseLogFileName);
						if (logFiles.Length > 0) {
							var lastFileInfo = logFiles
									.OrderByDescending(fInfo => fInfo.Name)
									.OrderByDescending(fInfo => fInfo.LastWriteTime).First();
							LogFileName = lastFileInfo.FullName;
						} else {
							// no files yet, use default name
							LogFileName = baseLogFileName;
						}
					} else {
						LogFileName = baseLogFileName;
					}
				} else {
					LogFileName = baseLogFileName;
				}
			}

			void createLogFileStream(bool append) {
				var fileInfo = new FileInfo(LogFileName);
				// Directory.Create will check if the directory already exists,
				// so there is no need for a "manual" check first.
				fileInfo.Directory.Create();

				LogFileStream = new FileStream(LogFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
				if (append) {
					LogFileStream.Seek(0, SeekOrigin.End);
				} else {
					LogFileStream.SetLength(0); // clear the file
				}
				LogFileWriter = new StreamWriter(LogFileStream);
			}

			internal void UseNewLogFile(string newLogFileName) {
				FileLogPrv.LogFileName = newLogFileName;
				DetermineLastFileLogName(); // preserve all existing logic related to 'FormatLogFileName' and rolling files
				createLogFileStream(FileLogPrv.Append);  // if file error occurs here it is not handled by 'HandleFileError' recursively
			}

			void OpenFile(bool append) {
				try {
					createLogFileStream(append);
				} catch (Exception ex) {
					if (FileLogPrv.HandleFileError != null) {
						var fileErrToNewName=FileLogPrv.HandleFileError(LogFileName, ex);
						if (fileErrToNewName != null) {
							UseNewLogFile(fileErrToNewName);
						}
					}
					else {
						throw; // do not handle by default to preserve backward compatibility
					}
				}
			}


			string GetNextFileLogName() {
				var baseLogFileName = GetBaseLogFileName();
				// if file does not exist or file size limit is not reached - do not add rolling file index
				if (!System.IO.File.Exists(baseLogFileName) ||
					FileLogPrv.FileSizeLimitBytes <= 0 ||
					new System.IO.FileInfo(baseLogFileName).Length < FileLogPrv.FileSizeLimitBytes)
					return baseLogFileName;

				switch (FileLogPrv.Options.RollingFilesConvention) {
					case FileLoggerOptions.FileRollingConvention.Ascending:
							//Unchanged default handling just optimized for performance and code reuse
							int currentFileIndex = GetIndexFromFile(baseLogFileName, LogFileName);
							var nextFileIndex = currentFileIndex + 1;
							if (FileLogPrv.MaxRollingFiles > 0) {
								nextFileIndex %= FileLogPrv.MaxRollingFiles;
							}
							return GetFileFromIndex(baseLogFileName, nextFileIndex);
					case FileLoggerOptions.FileRollingConvention.AscendingStableBase: {
							//Move current base file to next rolling file number
							RollingNumber++;
							if (FileLogPrv.MaxRollingFiles > 0) {
								RollingNumber %= FileLogPrv.MaxRollingFiles - 1;
							}
							var moveFile = GetFileFromIndex(baseLogFileName, RollingNumber + 1);
							if (System.IO.File.Exists(moveFile)) {
								System.IO.File.Delete(moveFile);
							}
							System.IO.File.Move(baseLogFileName, moveFile);
							return baseLogFileName;
						}
					case FileLoggerOptions.FileRollingConvention.Descending: {
							//Move all existing files to index +1 except if they are > MaxRollingFiles
							var logFiles = GetExistingLogFiles(baseLogFileName);
							if (logFiles.Length > 0) {
								foreach (var finfo in logFiles.OrderByDescending(fInfo => fInfo.Name)) {
									var index = GetIndexFromFile(baseLogFileName, finfo.Name);
									if (FileLogPrv.MaxRollingFiles > 0 && index >= FileLogPrv.MaxRollingFiles - 1) {
										continue;
									}
									var moveFile = GetFileFromIndex(baseLogFileName, index + 1);
									if (System.IO.File.Exists(moveFile)) {
										System.IO.File.Delete(moveFile);
									}
									System.IO.File.Move(finfo.FullName, moveFile);
								}
							}
							return baseLogFileName;
						}
				}
				throw new NotImplementedException("RollingFilesConvention");
			}

			// cache last returned base log file name to avoid excessive checks in CheckForNewLogFile.isBaseFileNameChanged
			string __LastBaseLogFileName = null;

			void CheckForNewLogFile() {
				bool openNewFile = false;
				if (isMaxFileSizeThresholdReached() || isBaseFileNameChanged())
					openNewFile = true;

				if (openNewFile) {
					Close();
					LogFileName = GetNextFileLogName();
					OpenFile(false);
				}

				bool isMaxFileSizeThresholdReached() {
					return FileLogPrv.FileSizeLimitBytes > 0 && LogFileStream.Length > FileLogPrv.FileSizeLimitBytes;
				}
				bool isBaseFileNameChanged() {
					if (FileLogPrv.FormatLogFileName != null) {
						var baseLogFileName = GetBaseLogFileName();
						if (baseLogFileName != __LastBaseLogFileName) {
							__LastBaseLogFileName = baseLogFileName;
							return true;
						}
						return false;
					}
					return false;
				}
			}

			internal void WriteMessage(string message, bool flush) {
				if (LogFileWriter != null) {
					CheckForNewLogFile();
					LogFileWriter.WriteLine(message);
					if (flush)
						LogFileWriter.Flush();
				}
			}

			/// <summary>
			/// Returns the index of a file or 0 if none found
			/// </summary>
			private int GetIndexFromFile(string baseLogFileName, string filename) {
#if NETSTANDARD2_0
				var baseFileNameOnly = Path.GetFileNameWithoutExtension(baseLogFileName);
				var currentFileNameOnly = Path.GetFileNameWithoutExtension(filename);

				var suffix = currentFileNameOnly.Substring(baseFileNameOnly.Length);
#else
				var baseFileNameOnly = Path.GetFileNameWithoutExtension(baseLogFileName.AsSpan());
				var currentFileNameOnly = Path.GetFileNameWithoutExtension(filename.AsSpan());

				var suffix = currentFileNameOnly.Slice(baseFileNameOnly.Length);
#endif
				if (suffix.Length > 0 && Int32.TryParse(suffix, out var parsedIndex)) {
					return parsedIndex;
				}
				return 0;
			}

			private string GetFileFromIndex(string baseLogFileName, int index) {
#if NETSTANDARD
				var nextFileName = Path.GetFileNameWithoutExtension(baseLogFileName) + (index > 0 ? index.ToString() : "") + Path.GetExtension(baseLogFileName);
				return Path.Combine(Path.GetDirectoryName(baseLogFileName), nextFileName);
#else
				// Contact for ReadOnlySpan<char> is not available in both netstandard2.0 and netstandard2.1
				var nextFileName = string.Concat(Path.GetFileNameWithoutExtension(baseLogFileName.AsSpan()), index > 0 ? index.ToString() : "", Path.GetExtension(baseLogFileName.AsSpan()));
				return string.Concat(Path.Join(Path.GetDirectoryName(baseLogFileName.AsSpan()), nextFileName.AsSpan()));
#endif
			}

			FileInfo[] GetExistingLogFiles(string baseLogFileName) {
				var logFileMask = Path.GetFileNameWithoutExtension(baseLogFileName) + "*" + Path.GetExtension(baseLogFileName);
				var logDirName = Path.GetDirectoryName(baseLogFileName);
				if (String.IsNullOrEmpty(logDirName))
					logDirName = Directory.GetCurrentDirectory();
				var logdir = new DirectoryInfo(logDirName);
				return logdir.Exists ? logdir.GetFiles(logFileMask, SearchOption.TopDirectoryOnly) : Array.Empty<FileInfo>();
			}

			internal void Close() {
				if (LogFileWriter != null) {
					var logWriter = LogFileWriter;
					LogFileWriter = null;

					logWriter.Dispose();
					LogFileStream.Dispose();
					LogFileStream = null;
				}

			}
		}

		/// <summary>
		/// Represents a file error context.
		/// </summary>
		public class FileError2 {

			/// <summary>
			/// Exception that occurs on the file operation.
			/// </summary>
			public Exception ErrorException { get; private set; }

			/// <summary>
			/// Current log file name.
			/// </summary>
			public string LogFileName { get; private set; }

			internal FileError2(string logFileName, Exception ex) {
				LogFileName = logFileName;
				ErrorException = ex;
			}

			internal string NewLogFileName { get; private set; }

			/// <summary>
			/// Suggests a new log file name to use instead of the current one. 
			/// </summary>
			/// <remarks>
			/// If proposed file name also leads to a file error this will break a file logger: errors are not handled recursively.
			/// </remarks>
			/// <param name="newLogFileName">a new log file name</param>
			public void UseNewLogFileName(string newLogFileName) {
				NewLogFileName = newLogFileName;
			}
		}

	}

}
