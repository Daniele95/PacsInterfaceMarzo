using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacsLibrary.Query
{
    public class CurrentConfiguration
    {
        public string ip { get; private set; }
        public int port { get; private set; }
        public string AET { get; private set; }
        public bool anonymizeData { get; private set; }
        public string thisNodeAET { get; private set; }
        public int thisNodePort { get; private set; }
        public string fileDestination { get; private set; }
        public bool useTls { get; private set; }
        public string keyStoreName { get; private set; }
        public string trustStorePath { get; private set; }
        public string trustStorePassword { get; private set; }


        public CurrentConfiguration()
        {
            update();
            restartListener();
            startConfigurationWatcher();
        }

        public void update()
        {
            bool locked = true;
            while (locked) locked = IsFileLocked(new FileInfo("ServerConfig.txt"));
            if (!locked)
            {
                var lines = File.ReadAllLines("ServerConfig.txt");
                var file = File.OpenRead("ServerConfig.txt");
                ip = lines[0];
                port = int.Parse(lines[1]);
                AET = lines[2];
                anonymizeData = bool.Parse(lines[3]);
                thisNodeAET = lines[4];
                thisNodePort = int.Parse(lines[5]);
                fileDestination = lines[6];
                useTls = bool.Parse(lines[7]);
                keyStoreName = lines[8];
                trustStorePath = lines[9];
                trustStorePassword = lines[10];
                file.Close();
            }
        }
        void restartListener()
        {
            Process[] listeners = Process.GetProcessesByName("Listener");
            if (listeners.Length != 0) foreach (var listener in listeners) listener.Kill();
            var newListener = new Process
            {
                StartInfo = {
                    FileName = "Listener",
                    Arguments = thisNodePort.ToString()+" "+useTls.ToString()
                }
            };
            newListener.Start();
        }
        void startConfigurationWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName("./"),
                Filter = Path.GetFileName("ServerConfig.txt"),
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += new FileSystemEventHandler((o, e) =>
            {
                int oldPort = thisNodePort;
                bool oldAnonymizeData = anonymizeData;
                update();
                if (thisNodePort != oldPort || anonymizeData != oldAnonymizeData) restartListener();

            });
        }
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try { stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None); }
            catch (IOException) { return true; }
            finally { if (stream != null) stream.Close(); }
            return false;
        }

    }

}
