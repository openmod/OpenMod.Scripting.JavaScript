using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;

[assembly: PluginMetadata("OpenMod.Scripting.JavaScript", DisplayName = "OpenMod JavaScript")]
namespace OpenMod.Scripting.JavaScript
{
    [UsedImplicitly]
    public class OpenModJavaScriptPlugin : OpenModUniversalPlugin
    {
        private readonly IRuntime m_Runtime;
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<OpenModJavaScriptPlugin> m_Logger;
        private readonly ILifetimeScope m_LifetimeScope;

        public OpenModJavaScriptPlugin(
            IRuntime runtime,
            IConfiguration configuration,
            ILogger<OpenModJavaScriptPlugin> logger,
            IServiceProvider serviceProvider,
            ILifetimeScope lifetimeScope) : base(serviceProvider)
        {
            m_Runtime = runtime;
            m_Configuration = configuration;
            m_Logger = logger;
            m_LifetimeScope = lifetimeScope;
        }

        protected override Task OnLoadAsync()
        {
            var scriptsDir = Path.Combine(m_Runtime.WorkingDirectory, "scripts");
            if (!Directory.Exists(scriptsDir))
            {
                Directory.CreateDirectory(scriptsDir);
            }
            else
            {
                foreach (var directory in Directory.GetDirectories(scriptsDir))
                {
                    var filePath = Path.Combine(directory, "startup.js");
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var flags = V8ScriptEngineFlags.EnableDateTimeConversion
                                | V8ScriptEngineFlags.EnableDynamicModuleImports;

                    if (m_Configuration.GetSection("enableDebugging").Exists() &&
                        m_Configuration.GetSection("enableDebugging").Get<bool>())
                    {
                        flags |= V8ScriptEngineFlags.EnableDebugging;
                    }

                    var scriptId = Path.GetDirectoryName(directory);

                    V8ScriptEngine engine = null;
                    var scriptLifeTime = m_LifetimeScope.BeginLifetimeScope(builder =>
                    {
                        engine = new V8ScriptEngine(scriptId, flags);
                        engine.AddHostObject("host", new ExtendedHostFunctions());
                        engine.AddHostObject("lib", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib",
                            "OpenMod.API",
                            "OpenMod.Core",
                            "OpenMod.Runtime",
                            "System",
                            "System.Core",
                            "System.Numerics",
                            "ClearScript"));

                        engine.SuppressExtensionMethodEnumeration = true;
                        engine.AllowReflection = true;

                        builder.Register(ctx => engine)
                            .As<V8ScriptEngine>()
                            .SingleInstance()
                            .OwnedByLifetimeScope();
                    });

                    try
                    {
                        var serviceProvider = scriptLifeTime.Resolve<IServiceProvider>();
                        engine.AddHostObject("openmod", ActivatorUtilities.CreateInstance<OpenModFunctions>(serviceProvider));
                        engine.Execute(new DocumentInfo(new Uri(filePath)), File.ReadAllText(filePath));
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex.GetBaseException(), $"Script error in script \"{scriptId}\"");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
