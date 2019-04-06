using Dicom;
using Dicom.Network;
using System;
using System.IO;
using System.Reflection;
using PacsLibrary.Listener;

namespace Listener
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            Console.Title = "Listener";
            int port = int.Parse(args[0]);
            string keyStoreName = File.ReadAllLines("ServerConfig.txt")[8];
            if (!bool.Parse(args[1])) keyStoreName = "";
            DicomServer.Create<CStoreSCP>(port, keyStoreName);
            Debug.startedListening(port);
            while (true) { }
        }
    }
}
