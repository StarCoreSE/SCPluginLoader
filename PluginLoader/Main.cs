﻿using VRage.Plugins;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System;
using System.IO;
using System.Security.Cryptography;
using avaness.PluginLoader.Data;
using SEPluginManager;
using VRage.FileSystem;
using System.Linq;
using Sandbox.Game;
using HarmonyLib;

namespace avaness.PluginLoader
{
    public class Main : IPlugin
    {
        public static Main Instance;
        
        public PluginConfig Config { get; }

        private readonly string mainPath;
        private LogFile log;

        public Main()
        {
            Instance = this;

            Cursor.Current = Cursors.WaitCursor;

            mainPath = Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "Plugins"));
            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            log = new LogFile(mainPath);
            log.WriteLine("Starting.");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony;

            log.WriteLine("Loading config.");
            Config = PluginConfig.Load(mainPath, log);

            new Harmony("avaness.PluginLoader").PatchAll();

            List<Assembly> assemblies = new List<Assembly>();
            bool error = false;
            foreach (PluginData data in Config.Data.Values)
            {
                try
                {
                    string name = data.Id;
                    if(data.Enabled)
                    {
                        log.WriteLine($"Loading {name}");
                        if (LoadDll(data, out Assembly a))
                        {
                            assemblies.Add(a);
                        }
                        else
                        {
                            error = true;
                            data.MarkError();
                        }
                    }
                    else
                    {
                        log.WriteLine($"Skipped {name}");
                    }
                }
                catch (Exception e)
                {
                    log.WriteLine("An error occurred:\n" + e);
                    data.MarkError();
                }
            }

            if(assemblies.Count > 0)
            {
                log.WriteLine($"Linking {assemblies.Count} assemblies to the game.");
                MethodInfo loadPlugins = typeof(MyPlugins).GetMethod("LoadPlugins", BindingFlags.NonPublic | BindingFlags.Static);
                try
                {
                    loadPlugins.Invoke(null, new object[] { assemblies });
                    ScanForSEPM();
                }
                catch(TargetInvocationException e)
                {
                    StringBuilder sb = new StringBuilder("An error occurred:");
                    sb.AppendLine();
                    ReflectionTypeLoadException inner = e.InnerException as ReflectionTypeLoadException;
                    if (inner == null)
                    {
                        sb.Append(e).AppendLine();
                    }
                    else
                    {
                        foreach (Exception le in inner.LoaderExceptions)
                            sb.Append(le).AppendLine();
                    }
                    log.WriteLine(sb.ToString());
                    error = true;
                }
                catch (Exception e)
                {
                    log.WriteLine("An error occurred:\n" + e);
                    error = true;
                }

            }
            else
            {
                log.WriteLine("No assemblies to link!");
            }

            log.WriteLine("Finished startup.");

            log.Flush();

            Cursor.Current = Cursors.Default;

            if (error)
                MessageBox.Show($"There was an error while trying to load a plugin. Some or all of the plugins may not have been loaded. See loader.log or the game log for details.", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool LoadDll(PluginData data, out Assembly a)
        {
            a = null;
            string dll = data.GetDllFile();
            if (dll == null)
            {
                log.WriteLine("Failed to load " + Path.GetFileName(dll));
                return false;
            }
            a = Assembly.LoadFile(dll);
            if(a.GetTypes().Any(t => typeof(IPlugin).IsAssignableFrom(t)))
                return true;
            log.WriteLine($"Failed to load {Path.GetFileName(dll)} because it does not contain an IPlugin.");
            return false;
        }

        private void ScanForSEPM()
        {
            foreach(IPlugin p in MyPlugins.Plugins)
            {
                if (p is SEPluginManager.SEPMPlugin sepm)
                    ExecuteMain(sepm);
            }
        }

        private Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            string assembly = args.RequestingAssembly?.GetName()?.ToString();
            if(args.Name.Contains("0Harmony"))
            {
                if(assembly != null)
                    log.WriteLine("Resolving 0Harmony for " + assembly);
                else
                    log.WriteLine("Resolving 0Harmony");
                return typeof(Harmony).Assembly;
            }
            else if(args.Name.Contains("SEPluginManager"))
            {
                if (assembly != null)
                    log.WriteLine("Resolving SEPluginManager for " + assembly);
                else
                    log.WriteLine("Resolving SEPluginManager");
                return typeof(Main).Assembly;
            }
            return null;
        }

        private void ExecuteMain(SEPluginManager.SEPMPlugin plugin)
        {
            try
            {
                string name = plugin.GetType().ToString();
                log.WriteLine("Executing Main of " + name);
                plugin.Main(new Harmony(name), new Logger(name, log));
            }
            catch (Exception e)
            {
                log.WriteLine("Error while calling SEPM Main: " + e);
            }
        }

        public void Init(object gameInstance)
        { }

        public void Update()
        { }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
            Instance = null;
            log?.Dispose();
            log = null;
        }
    }
}