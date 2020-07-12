using System;
using System.Linq;
using Autofac.Util;
using Microsoft.Extensions.DependencyInjection;
using V8.Net;

namespace OpenMod.Scripting.JavaScript
{
    [ScriptObject]
    public class OpenModFunctions
    {
        private readonly IServiceProvider m_ServiceProvider;

        public OpenModFunctions(IServiceProvider serviceProvider)
        {
            m_ServiceProvider = serviceProvider;
        }

        [ScriptMember("getService", ScriptMemberSecurity.Locked)]
        public object GetService(Type type)
        {
            return m_ServiceProvider.GetService(type);
        }

        [ScriptMember("getService", ScriptMemberSecurity.Locked)]
        public object GetService(string typeName)
        {
            return m_ServiceProvider.GetService(GetType(typeName));
        }

        [ScriptMember("getRequiredService", ScriptMemberSecurity.Locked)]
        public object GetRequiredService(Type type)
        {
            return m_ServiceProvider.GetRequiredService(type);
        }

        [ScriptMember("getRequiredService", ScriptMemberSecurity.Locked)]
        public object GetRequiredService(string typeName)
        {
            return m_ServiceProvider.GetRequiredService(GetType(typeName));
        }

        [ScriptMember("getType", ScriptMemberSecurity.Locked)]
        public Type GetType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetLoadableTypes()).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }
    }
}