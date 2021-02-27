﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VRage.Plugins;
using VRage.Utils;

namespace avaness.RunPluginLoader
{
    public class Main : IPlugin
    {
        private IPlugin pluginLoader;
        private Assembly harmony;

        public Main()
        {
            Log("Loading PluginLoader and dependences...");
            try
            {
                string dir = AssemblyDirectory;
                harmony = Assembly.LoadFile(Path.Combine(dir, "0Harmony"));
                AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony;
                Assembly pluginLoaderAssembly = Assembly.LoadFile(Path.Combine(dir, "PluginLoader"));
                Type pluginType = typeof(IPlugin);
                IEnumerable<Type> types = pluginLoaderAssembly.GetTypes().Where(t => pluginType.IsAssignableFrom(t) && t.Name.Contains("Main"));
                if (types.Any())
                {
                    pluginLoader = (IPlugin)Activator.CreateInstance(types.First());
                    Log($"PluginLoader started.");
                }
                else
                {
                    Log("Failed to find PluginLoader!");
                }
            }
            catch (Exception e) 
            {
                Log("Error: " + e);
            }
        }

        private void Log(string s)
        {
            MyLog.Default.WriteLine("[RunPluginLoader] " + s);
        }

        private Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            if(args.Name.Contains("0Harmony"))
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
                Log("0Harmony dependency loaded.");
                return harmony;
            }
            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
            pluginLoader?.Dispose();
        }

        public void Init(object gameInstance)
        {
            pluginLoader?.Init(gameInstance);
        }

        public void Update()
        {
            pluginLoader?.Update();
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetFullPath(Path.GetDirectoryName(path));
            }
        }
    }
}