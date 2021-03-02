﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace avaness.PluginLoader
{
    public class LogFile : IDisposable
    {
        private const string fileName = "loader.log";
        private StreamWriter writer;


        public void WriteLine(string text, string prefix = null)
        {
            if(prefix == null)
            {
                writer?.WriteLine($"{DateTime.Now} {text}");
                MyLog.Default.WriteLine($"[PluginLoader] {text}");
            }
            else
            {
                writer?.WriteLine($"{DateTime.Now} [{prefix}] {text}");
                MyLog.Default.WriteLine($"[{prefix}] {text}");
            }
        }

        public LogFile(string mainPath)
        {
            string file = Path.Combine(mainPath, fileName);
            writer = File.CreateText(file);
        }

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer = null;
            }

        }
        
        public void Flush()
        {
            writer.Flush();
        }
    }
}
