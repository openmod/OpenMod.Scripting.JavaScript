using Microsoft.Extensions.Logging;
using V8.Net;

#pragma warning disable IDE1006 // Naming Styles

namespace OpenMod.Scripting.JavaScript
{
    [ScriptObject]
    public class ScriptLogger
    {
        private readonly ILogger m_Logger;

        public ScriptLogger(ILoggerFactory loggerFactory, string scriptId)
        {
            m_Logger = loggerFactory.CreateLogger($"JavaScript.{scriptId}");
        }

        [ScriptMember("info")]
        public void Info(string message)
        {
            m_Logger.LogInformation(message);
        }

        [ScriptMember("warn")]
        public void Warn(string message)
        {
            m_Logger.LogWarning(message);
        }

        [ScriptMember("trace")]
        public void Trace(string message)
        {
            m_Logger.LogTrace(message);
        }

        [ScriptMember("critical")]
        public void Critical(string message)
        {
            m_Logger.LogCritical(message);
        }

        [ScriptMember("err")]
        public void Err(string message)
        {
            m_Logger.LogError(message);
        }

        [ScriptMember("dbg")]
        public void Dbg(string message)
        {
            m_Logger.LogDebug(message);
        }
    }
}