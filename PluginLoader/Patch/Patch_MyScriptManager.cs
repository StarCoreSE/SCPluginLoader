﻿using HarmonyLib;
using Sandbox.Game.World;
using System;
using VRage.Game;
using System.Linq;
using System.Reflection;
using avaness.PluginLoader.Data;
using System.Collections.Generic;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyScriptManager), "LoadData")]
    public static class Patch_MyScripManager
    {
        private static Action<MyScriptManager, string, MyModContext> loadScripts;

        static Patch_MyScripManager()
        {
            loadScripts = (Action<MyScriptManager, string, MyModContext>)Delegate.CreateDelegate(typeof(Action<MyScriptManager, string, MyModContext>), typeof(MyScriptManager).GetMethod("LoadScripts", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public static void Postfix(MyScriptManager __instance)
        {
            try
            {
                HashSet<ulong> currentMods;
                if (MySession.Static.Mods != null)
                    currentMods = new HashSet<ulong>(MySession.Static.Mods.Select(x => x.PublishedFileId));
                else
                    currentMods = new HashSet<ulong>();

                foreach (PluginData data in Main.Instance.Config.EnabledPlugins)
                {
                    if (data is ModPlugin mod && !currentMods.Contains(mod.WorkshopId) && mod.Exists)
                    {
                        LogFile.WriteLine("Loading client mod scripts for " + mod.WorkshopId);
                        loadScripts(__instance, mod.ModLocation, mod.GetModContext());
                    }
                }
            }
            catch (Exception e)
            {
                LogFile.WriteLine("An error occured while loading client mods: " + e);
                throw;
            }
        }
    }
}
