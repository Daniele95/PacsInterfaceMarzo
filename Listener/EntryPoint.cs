using Dicom.Network;
using System;
using System.IO;
using System.Threading;

namespace Listener
{
    class CurrentListener
    {
        bool changed = false;

        public CurrentListener()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName("./");
            watcher.Filter = Path.GetFileName("ServerConfig.txt");
            watcher.Changed += new FileSystemEventHandler((o,e)=> { changed = true; });
            watcher.EnableRaisingEvents = true;

            int portToListenOn = int.Parse(File.ReadAllLines("ServerConfig.txt")[5]);
            IDicomServer listener = DicomServer.Create<CStoreSCP>(portToListenOn);
            Console.WriteLine("Started listening for incoming datasets on port " + portToListenOn);

            while (true) {
                Thread.Sleep(1000);
                if(changed && !IsFileLocked(new FileInfo("ServerConfig.txt")))
                {
                    portToListenOn = int.Parse(File.ReadAllLines("ServerConfig.txt")[5]);
                    listener = DicomServer.Create<CStoreSCP>(portToListenOn);
                    Console.WriteLine("Started listening for incoming datasets on port " + portToListenOn);
                    changed = false;
                }
            }
        }
        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }

    class EntryPoint
    {
        static void Main(string[] args)
        {
            var currentListener = new CurrentListener();
        }
    }
}
