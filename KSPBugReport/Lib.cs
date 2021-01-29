using System.Diagnostics;
using System.Reflection;

namespace KSPBugReport
{
    public static class Lib
    {
		public enum LogLevel
		{
			Message,
			Warning,
			Error
		}

		private static void Log(MethodBase method, string message, LogLevel level)
		{
			switch (level)
			{
				default:
					UnityEngine.Debug.Log($"[KSPBugReport] {method.ReflectedType.Name}.{method.Name} {message}");
					return;
				case LogLevel.Warning:
					UnityEngine.Debug.LogWarning($"[KSPBugReport] {method.ReflectedType.Name}.{method.Name} {message}");
					return;
				case LogLevel.Error:
					UnityEngine.Debug.LogError($"[KSPBugReport] {method.ReflectedType.Name}.{method.Name} {message}");
					return;
			}
		}

		///<summary>write a message to the log</summary>
		public static void Log(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the call stack to the log</summary>
		public static void LogStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);

			// KSP will already log the stacktrace if the log level is error
			if (level != LogLevel.Error)
				UnityEngine.Debug.Log(stackTrace);
		}

		///<summary>write a message to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		public static void LogDebug(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the full call stack to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		public static void LogDebugStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);

			// KSP will already log the stacktrace if the log level is error
			if (level != LogLevel.Error)
				UnityEngine.Debug.Log(stackTrace);
		}

        public static bool IsGameRunning
        {
            get
            {
                switch (HighLogic.LoadedScene)
                {
                    case GameScenes.SPACECENTER:
                    case GameScenes.FLIGHT:
                    case GameScenes.TRACKSTATION:
                        return true;
                    default:
                        return false;
                }
            }
        }
	}
}
