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
            Configuration configuration = new Configuration("ServerConfig.txt");
            PacsLibrary.Listener.Listener.init(configuration);
        }
    }
}
