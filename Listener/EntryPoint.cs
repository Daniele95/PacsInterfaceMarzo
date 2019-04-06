using Dicom;
using Dicom.Network;
using System;
using System.IO;
using System.Reflection;
using PacsLibrary.Listener;
using PacsLibrary;
using System.Security.Cryptography.X509Certificates;

namespace Listener
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            Console.Title = "Listener";
            int port = int.Parse(args[0]);
            string keyStoreName = args[1];
            DicomServer.Create<CStoreSCP>(port, args[1]);
            Debug.startedListening(port);
            while (true) { }
        }
    }
}
