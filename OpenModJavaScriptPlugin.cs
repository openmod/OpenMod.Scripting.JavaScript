using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using OpenMod.API;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using OpenMod.NuGet;
using V8.Net;

[assembly: PluginMetadata("OpenMod.Scripting.JavaScript", DisplayName = "OpenMod JavaScript")]
namespace OpenMod.Scripting.JavaScript
{
    [UsedImplicitly]
    public class OpenModJavaScriptPlugin : OpenModUniversalPlugin
    {
        private readonly IRuntime m_Runtime;
        private readonly ILogger<OpenModJavaScriptPlugin> m_Logger;
        private readonly ILifetimeScope m_LifetimeScope;
        private readonly NuGetPackageManager m_NuGetPackageManager;

        private static bool s_Initialized;
        private V8Engine m_Engine;

        public OpenModJavaScriptPlugin(
            IRuntime runtime,
            IConfiguration configuration,
            ILogger<OpenModJavaScriptPlugin> logger,
            IServiceProvider serviceProvider,
            ILifetimeScope lifetimeScope,
            NuGetPackageManager nuGetPackageManager) : base(serviceProvider)
        {
            m_Runtime = runtime;
            m_Logger = logger;
            m_LifetimeScope = lifetimeScope;
            m_NuGetPackageManager = nuGetPackageManager;
        }

        protected override async Task OnLoadAsync()
        {
            var scriptsDir = Path.Combine(m_Runtime.WorkingDirectory, "scripts");
            var nativeDir = Path.Combine(m_Runtime.WorkingDirectory, "native");

            if (!s_Initialized)
            {
                if (!Directory.Exists(nativeDir))
                {
                    Directory.CreateDirectory(nativeDir);
                }

                var latestPackageId = await m_NuGetPackageManager.GetLatestPackageIdentityAsync("V8.NET");
                var nupkgFile = m_NuGetPackageManager.GetNugetPackageFile(latestPackageId);
                var packageReader = new PackageArchiveReader(nupkgFile);
                foreach (var file in packageReader.GetFiles("contentFiles"))
                {
                    if (!file.Contains("netstandard2.0"))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(file);
                    var entry = packageReader.GetEntry(file);
                    using var stream = entry.Open();
                    var ms = new FileStream(Path.Combine(nativeDir, fileName), FileMode.Create);
                    await stream.CopyToAsync(ms);

                    ms.Close();
                    stream.Close();
                }

                Loader.AlternateRootSubPath = nativeDir;
                s_Initialized = true;
            }

            if (!Directory.Exists(scriptsDir))
            {
                Directory.CreateDirectory(scriptsDir);
            }
            else
            {
                foreach (var directory in Directory.GetDirectories(scriptsDir))
                {
                    m_Logger.LogInformation($"[loading] Script: {directory}");

                    var filePath = Path.Combine(directory, "startup.js");
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    var scriptId = Path.GetDirectoryName(directory);

                    m_Engine = new V8Engine(true);
                    var globalContext = m_Engine.GetContext();
                    var v8Context = m_Engine.CreateContext();

                    var scriptLifeTime = m_LifetimeScope.BeginLifetimeScope(builder =>
                    {
                        builder.Register(ctx => m_Engine)
                            .As<V8Engine>()
                            .SingleInstance()
                            .OwnedByLifetimeScope();

                        builder.Register(ctx => v8Context)
                            .As<Context>()
                            .SingleInstance()
                            .OwnedByLifetimeScope();
                    });

                    try
                    {
                        var serviceProvider = scriptLifeTime.Resolve<IServiceProvider>();
                        m_Engine.SetContext(v8Context);
                        m_Engine.GlobalObject.SetProperty("logger", ActivatorUtilities.CreateInstance<ScriptLogger>(serviceProvider, scriptId), memberSecurity: ScriptMemberSecurity.Locked);
                        m_Engine.GlobalObject.SetProperty("openmod", ActivatorUtilities.CreateInstance<OpenModFunctions>(serviceProvider), memberSecurity: ScriptMemberSecurity.Locked);
                        var script = m_Engine.LoadScript(filePath, throwExceptionOnError: true);
                        m_Engine.Execute(script, throwExceptionOnError: true, trackReturn: false);
                        m_Engine.SetContext(globalContext); 
                        GC.KeepAlive(script);
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex.GetBaseException(), $"Script error in script \"{scriptId}\"");
                    }
                }
            }
        }
    }
}
