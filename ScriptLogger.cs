using Microsoft.Extensions.Logging;

namespace OpenMod.Scripting.JavaScript
{
    public class ScriptLogger
    {
        private readonly ILogger m_Logger;

        public ScriptLogger(ILoggerFactory loggerFactory, string scriptId)
        {
            m_Logger = loggerFactory.CreateLogger($"Script[{scriptId}]");
        }

        public void info(string message)
        {
            m_Logger.LogInformation(message);
        }

        public void warn(string message)
        {
            m_Logger.LogWarning(message);
        }

        public void trace(string message)
        {
            m_Logger.LogTrace(message);
        }

        public void critical(string message)
        {
            m_Logger.LogCritical(message);
        }

        public void err(string message)
        {
            m_Logger.LogError(message);
        }

        public void dbg(string message)
        {
            m_Logger.LogDebug(message);
        }
    }
}