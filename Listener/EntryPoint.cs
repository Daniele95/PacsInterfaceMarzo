using Dicom.Network;
using System;

namespace Listener
{

    class EntryPoint
    {
        static void Main(string[] args)
        {
            Console.Title = "Listener";
            int port = int.Parse(args[0]);
            DicomServer.Create<CStoreSCP>(port);
            Console.WriteLine("Started listening for incoming datasets on port " + port);
            while (true) { }
        }
    }
}
