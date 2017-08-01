using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Net
{
	internal class Logging
	{
		private class NclTraceSource : TraceSource
		{
			internal NclTraceSource(string name) : base(name)
			{
			}

			protected override string[] GetSupportedAttributes()
			{
				return Logging.SupportedAttributes;
			}
		}

		private const int DefaultMaxDumpSize = 1024;

		private const bool DefaultUseProtocolTextOnly = false;

		private const string AttributeNameMaxSize = "maxdatasize";

		private const string AttributeNameTraceMode = "tracemode";

		private const string AttributeValueProtocolOnly = "protocolonly";

		private const string TraceSourceWebName = "System.Net";

		private const string TraceSourceHttpListenerName = "System.Net.HttpListener";

		private const string TraceSourceSocketsName = "System.Net.Sockets";

		private const string TraceSourceWebSocketsName = "System.Net.WebSockets";

		private const string TraceSourceCacheName = "System.Net.Cache";

		private const string TraceSourceHttpName = "System.Net.Http";

		private static volatile bool s_LoggingEnabled = true;

		private static volatile bool s_LoggingInitialized;

		private static volatile bool s_AppDomainShutdown;

		private static readonly string[] SupportedAttributes = new string[]
		{
			"maxdatasize",
			"tracemode"
		};

		private static TraceSource s_WebTraceSource;

		private static TraceSource s_HttpListenerTraceSource;

		private static TraceSource s_SocketsTraceSource;

		private static TraceSource s_WebSocketsTraceSource;

		private static TraceSource s_CacheTraceSource;

		private static TraceSource s_TraceSourceHttpName;

		private static object s_InternalSyncObject;

		private static Encoding headerEncoding = Encoding.GetEncoding(28591);

		private static object InternalSyncObject
		{
			get
			{
				if (Logging.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange(ref Logging.s_InternalSyncObject, value, null);
				}
				return Logging.s_InternalSyncObject;
			}
		}

		internal static bool On
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				return Logging.s_LoggingEnabled;
			}
		}

		internal static TraceSource Web
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_WebTraceSource;
			}
		}

		internal static TraceSource Http
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_TraceSourceHttpName;
			}
		}

		internal static TraceSource HttpListener
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_HttpListenerTraceSource;
			}
		}

		internal static TraceSource Sockets
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_SocketsTraceSource;
			}
		}

		internal static TraceSource RequestCache
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_CacheTraceSource;
			}
		}

		internal static TraceSource WebSockets
		{
			get
			{
				if (!Logging.s_LoggingInitialized)
				{
					Logging.InitializeLogging();
				}
				if (!Logging.s_LoggingEnabled)
				{
					return null;
				}
				return Logging.s_WebSocketsTraceSource;
			}
		}

		private Logging()
		{
		}

		internal static bool IsVerbose(TraceSource traceSource)
		{
			return Logging.ValidateSettings(traceSource, TraceEventType.Verbose);
		}

		private static bool GetUseProtocolTextSetting(TraceSource traceSource)
		{
			bool result = false;
			if (traceSource.Attributes["tracemode"] == "protocolonly")
			{
				result = true;
			}
			return result;
		}

		private static int GetMaxDumpSizeSetting(TraceSource traceSource)
		{
			int result = 1024;
			if (traceSource.Attributes.ContainsKey("maxdatasize"))
			{
				try
				{
					result = int.Parse(traceSource.Attributes["maxdatasize"], NumberFormatInfo.InvariantInfo);
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
					{
						throw;
					}
					traceSource.Attributes["maxdatasize"] = result.ToString(NumberFormatInfo.InvariantInfo);
				}
			}
			return result;
		}

		[SecuritySafeCritical]
		private static void InitializeLogging()
		{
			lock (Logging.InternalSyncObject)
			{
				if (!Logging.s_LoggingInitialized)
				{
					bool flag2 = false;
					if (typeof(Logging).Assembly.IsFullyTrusted)
					{
						Logging.s_WebTraceSource = new Logging.NclTraceSource("System.Net");
						Logging.s_HttpListenerTraceSource = new Logging.NclTraceSource("System.Net.HttpListener");
						Logging.s_SocketsTraceSource = new Logging.NclTraceSource("System.Net.Sockets");
						Logging.s_WebSocketsTraceSource = new Logging.NclTraceSource("System.Net.WebSockets");
						Logging.s_CacheTraceSource = new Logging.NclTraceSource("System.Net.Cache");
						Logging.s_TraceSourceHttpName = new Logging.NclTraceSource("System.Net.Http");
						try
						{
							flag2 = (Logging.s_WebTraceSource.Switch.ShouldTrace(TraceEventType.Critical) || Logging.s_HttpListenerTraceSource.Switch.ShouldTrace(TraceEventType.Critical) || Logging.s_SocketsTraceSource.Switch.ShouldTrace(TraceEventType.Critical) || Logging.s_WebSocketsTraceSource.Switch.ShouldTrace(TraceEventType.Critical) || Logging.s_CacheTraceSource.Switch.ShouldTrace(TraceEventType.Critical) || Logging.s_TraceSourceHttpName.Switch.ShouldTrace(TraceEventType.Critical));
						}
						catch (SecurityException)
						{
							Logging.Close();
							flag2 = false;
						}
					}
					if (flag2)
					{
						AppDomain currentDomain = AppDomain.CurrentDomain;
						currentDomain.UnhandledException += new UnhandledExceptionEventHandler(Logging.UnhandledExceptionHandler);
						currentDomain.DomainUnload += new EventHandler(Logging.AppDomainUnloadEvent);
						currentDomain.ProcessExit += new EventHandler(Logging.ProcessExitEvent);
					}
					Logging.s_LoggingEnabled = flag2;
					Logging.s_LoggingInitialized = true;
				}
			}
		}

		[SecurityCritical]
		private static void Close()
		{
			if (Logging.s_WebTraceSource != null)
			{
				Logging.s_WebTraceSource.Close();
			}
			if (Logging.s_HttpListenerTraceSource != null)
			{
				Logging.s_HttpListenerTraceSource.Close();
			}
			if (Logging.s_SocketsTraceSource != null)
			{
				Logging.s_SocketsTraceSource.Close();
			}
			if (Logging.s_WebSocketsTraceSource != null)
			{
				Logging.s_WebSocketsTraceSource.Close();
			}
			if (Logging.s_CacheTraceSource != null)
			{
				Logging.s_CacheTraceSource.Close();
			}
			if (Logging.s_TraceSourceHttpName != null)
			{
				Logging.s_TraceSourceHttpName.Close();
			}
		}

		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			Exception e = (Exception)args.ExceptionObject;
			Logging.Exception(Logging.Web, sender, "UnhandledExceptionHandler", e);
		}

		[SecurityCritical]
		private static void ProcessExitEvent(object sender, EventArgs e)
		{
			Logging.Close();
			Logging.s_AppDomainShutdown = true;
		}

		[SecurityCritical]
		private static void AppDomainUnloadEvent(object sender, EventArgs e)
		{
			Logging.Close();
			Logging.s_AppDomainShutdown = true;
		}

		private static bool ValidateSettings(TraceSource traceSource, TraceEventType traceLevel)
		{
			if (!Logging.s_LoggingEnabled)
			{
				return false;
			}
			if (!Logging.s_LoggingInitialized)
			{
				Logging.InitializeLogging();
			}
			return traceSource != null && traceSource.Switch.ShouldTrace(traceLevel) && !Logging.s_AppDomainShutdown;
		}

		private static string GetObjectName(object obj)
		{
			if (obj is Uri || obj is IPAddress || obj is IPEndPoint)
			{
				return obj.ToString();
			}
			return obj.GetType().Name;
		}

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetCurrentThreadId();

		[SecurityCritical]
		internal static uint GetThreadId()
		{
			uint num = Logging.GetCurrentThreadId();
			if (num == 0u)
			{
				num = (uint)Thread.CurrentThread.GetHashCode();
			}
			return num;
		}

		[SecuritySafeCritical]
		internal static void PrintLine(TraceSource traceSource, TraceEventType eventType, int id, string msg)
		{
			string str = "[" + Logging.GetThreadId().ToString("d4", CultureInfo.InvariantCulture) + "] ";
			traceSource.TraceEvent(eventType, id, str + msg);
		}

		internal static void Associate(TraceSource traceSource, object objA, object objB)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			string str = Logging.GetObjectName(objA) + "#" + ValidationHelper.HashString(objA);
			string str2 = Logging.GetObjectName(objB) + "#" + ValidationHelper.HashString(objB);
			Logging.PrintLine(traceSource, TraceEventType.Information, 0, "Associating " + str + " with " + str2);
		}

		internal static void Enter(TraceSource traceSource, object obj, string method, string param)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Enter(traceSource, Logging.GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, param);
		}

		internal static void Enter(TraceSource traceSource, object obj, string method, object paramObject)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Enter(traceSource, Logging.GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, paramObject);
		}

		internal static void Enter(TraceSource traceSource, string obj, string method, string param)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Enter(traceSource, string.Concat(new string[]
			{
				obj,
				"::",
				method,
				"(",
				param,
				")"
			}));
		}

		internal static void Enter(TraceSource traceSource, string obj, string method, object paramObject)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			string text = "";
			if (paramObject != null)
			{
				text = Logging.GetObjectName(paramObject) + "#" + ValidationHelper.HashString(paramObject);
			}
			Logging.Enter(traceSource, string.Concat(new string[]
			{
				obj,
				"::",
				method,
				"(",
				text,
				")"
			}));
		}

		internal static void Enter(TraceSource traceSource, string method, string parameters)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Enter(traceSource, method + "(" + parameters + ")");
		}

		internal static void Enter(TraceSource traceSource, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, msg);
		}

		internal static void Exit(TraceSource traceSource, object obj, string method, object retObject)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			string retValue = "";
			if (retObject != null)
			{
				retValue = Logging.GetObjectName(retObject) + "#" + ValidationHelper.HashString(retObject);
			}
			Logging.Exit(traceSource, obj, method, retValue);
		}

		internal static void Exit(TraceSource traceSource, string obj, string method, object retObject)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			string retValue = "";
			if (retObject != null)
			{
				retValue = Logging.GetObjectName(retObject) + "#" + ValidationHelper.HashString(retObject);
			}
			Logging.Exit(traceSource, obj, method, retValue);
		}

		internal static void Exit(TraceSource traceSource, object obj, string method, string retValue)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Exit(traceSource, Logging.GetObjectName(obj) + "#" + ValidationHelper.HashString(obj), method, retValue);
		}

		internal static void Exit(TraceSource traceSource, string obj, string method, string retValue)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			if (!ValidationHelper.IsBlankString(retValue))
			{
				retValue = "\t-> " + retValue;
			}
			Logging.Exit(traceSource, string.Concat(new string[]
			{
				obj,
				"::",
				method,
				"() ",
				retValue
			}));
		}

		internal static void Exit(TraceSource traceSource, string method, string parameters)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.Exit(traceSource, method + "() " + parameters);
		}

		internal static void Exit(TraceSource traceSource, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, "Exiting " + msg);
		}

		internal static void Exception(TraceSource traceSource, object obj, string method, Exception e)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Error))
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(e.Message);
			if (e is AggregateException)
			{
				AggregateException ex = e as AggregateException;
				ReadOnlyCollection<Exception> innerExceptions = ex.Flatten().InnerExceptions;
				if (innerExceptions.Count > 0)
				{
					string text = string.Join(", ", from innerException in innerExceptions
					select innerException.Message);
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " InnerExceptions: {0}", new object[]
					{
						text
					});
				}
			}
			string text2 = string.Format(CultureInfo.InvariantCulture, SysSR.net_log_exception, new object[]
			{
				Logging.GetObjectLogHash(obj),
				method,
				stringBuilder.ToString()
			});
			if (!ValidationHelper.IsBlankString(e.StackTrace))
			{
				text2 = text2 + "\r\n" + e.StackTrace;
			}
			Logging.PrintLine(traceSource, TraceEventType.Error, 0, text2);
		}

		internal static void PrintInfo(TraceSource traceSource, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Information, 0, msg);
		}

		internal static void PrintInfo(TraceSource traceSource, object obj, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Information, 0, string.Concat(new string[]
			{
				Logging.GetObjectName(obj),
				"#",
				ValidationHelper.HashString(obj),
				" - ",
				msg
			}));
		}

		internal static void PrintInfo(TraceSource traceSource, object obj, string method, string param)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Information))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Information, 0, string.Concat(new string[]
			{
				Logging.GetObjectName(obj),
				"#",
				ValidationHelper.HashString(obj),
				"::",
				method,
				"(",
				param,
				")"
			}));
		}

		internal static void PrintWarning(TraceSource traceSource, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Warning))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Warning, 0, msg);
		}

		internal static void PrintWarning(TraceSource traceSource, object obj, string method, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Warning))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Warning, 0, string.Concat(new string[]
			{
				Logging.GetObjectName(obj),
				"#",
				ValidationHelper.HashString(obj),
				"::",
				method,
				"() - ",
				msg
			}));
		}

		internal static void PrintError(TraceSource traceSource, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Error))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Error, 0, msg);
		}

		internal static void PrintError(TraceSource traceSource, object obj, string method, string msg)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Error))
			{
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Error, 0, string.Concat(new string[]
			{
				Logging.GetObjectName(obj),
				"#",
				ValidationHelper.HashString(obj),
				"::",
				method,
				"() - ",
				msg
			}));
		}

		internal static string GetObjectLogHash(object obj)
		{
			return Logging.GetObjectName(obj) + "#" + ValidationHelper.HashString(obj);
		}

		internal static void Dump(TraceSource traceSource, object obj, string method, byte[] buffer, int offset, int length)
		{
			if (!Logging.ValidateSettings(traceSource, TraceEventType.Verbose))
			{
				return;
			}
			if (buffer == null)
			{
				Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, "(null)");
				return;
			}
			if (offset > buffer.Length)
			{
				Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, "(offset out of range)");
				return;
			}
			Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, string.Concat(new string[]
			{
				"Data from ",
				Logging.GetObjectName(obj),
				"#",
				ValidationHelper.HashString(obj),
				"::",
				method
			}));
			int maxDumpSizeSetting = Logging.GetMaxDumpSizeSetting(traceSource);
			if (length > maxDumpSizeSetting)
			{
				Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, string.Concat(new string[]
				{
					"(printing ",
					maxDumpSizeSetting.ToString(NumberFormatInfo.InvariantInfo),
					" out of ",
					length.ToString(NumberFormatInfo.InvariantInfo),
					")"
				}));
				length = maxDumpSizeSetting;
			}
			if (length < 0 || length > buffer.Length - offset)
			{
				length = buffer.Length - offset;
			}
			if (Logging.GetUseProtocolTextSetting(traceSource))
			{
				string msg = "<<" + Logging.headerEncoding.GetString(buffer, offset, length) + ">>";
				Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, msg);
				return;
			}
			do
			{
				int num = Math.Min(length, 16);
				string text = string.Format(CultureInfo.CurrentCulture, "{0:X8} : ", new object[]
				{
					offset
				});
				for (int i = 0; i < num; i++)
				{
					text = text + string.Format(CultureInfo.CurrentCulture, "{0:X2}", new object[]
					{
						buffer[offset + i]
					}) + ((i == 7) ? '-' : ' ');
				}
				for (int j = num; j < 16; j++)
				{
					text += "   ";
				}
				text += ": ";
				for (int k = 0; k < num; k++)
				{
					text += (char)((buffer[offset + k] < 32 || buffer[offset + k] > 126) ? 46 : buffer[offset + k]);
				}
				Logging.PrintLine(traceSource, TraceEventType.Verbose, 0, text);
				offset += num;
				length -= num;
			}
			while (length > 0);
		}
	}
}
