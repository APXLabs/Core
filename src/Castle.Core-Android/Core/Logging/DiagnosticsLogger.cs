// Copyright 2004-2015 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Core.Logging
{
	using System;
	using System.Diagnostics;
	using System.Globalization;

	/// <summary>
	///   The Logger using Android's logcat.
	/// </summary>
	#if FEATURE_SERIALIZATION
	[Serializable]
	#endif
	public class DiagnosticsLogger : LevelFilteredLogger
	{
		/// <summary>
		///   Creates a logger that uses Android's logcat.
		/// </summary>
		/// <param name = "tag">The identifying tag for messages from this logger.</param>
		public DiagnosticsLogger(string tag) : this(String.Empty, tag)
		{
		}

		/// <summary>
		///   Creates a logger that uses Android's logcat.
		/// </summary>
		/// <param name = "ignored">Unused.</param>
		/// <param name = "tag">The identifying tag for messages from this logger.</param>
		public DiagnosticsLogger(string ignored, string tag) : base(LoggerLevel.Debug)
		{
			ChangeName(tag);
		}

		public override ILogger CreateChildLogger(string loggerName)
		{
			return new DiagnosticsLogger(String.Format("{0}.{1}", Name, loggerName));
		}

		protected override void Log(LoggerLevel loggerLevel, string loggerName, string message, Exception exception)
		{
			string contentToLog;

			if (exception == null)
			{
				contentToLog = message;
			}
			else
			{
				contentToLog = string.Format(CultureInfo.CurrentCulture, "message: {0} exception: {1} {2} {3}",
					message, exception.GetType(), exception.Message, exception.StackTrace);
			}

			switch (loggerLevel)
			{
			case LoggerLevel.Fatal:
				Android.Util.Log.Wtf(loggerName, contentToLog);
				break;
			case LoggerLevel.Error:
				Android.Util.Log.Error(loggerName, contentToLog);
				break;
			case LoggerLevel.Warn:
				Android.Util.Log.Warn(loggerName, contentToLog);
				break;
			case LoggerLevel.Info:
				Android.Util.Log.Info(loggerName, contentToLog);
				break;
			case LoggerLevel.Debug:
				Android.Util.Log.Debug(loggerName, contentToLog);
				break;
			case LoggerLevel.Off:
			default:
				Android.Util.Log.Verbose(loggerName, contentToLog);
				break;
			}
		}

	}
}
