using Dicom;
using Dicom.Network;
using System;
using System.IO;
using System.Reflection;

namespace Listener
{
    class Debug
    {
        public const bool verbose = false;
        
        public static void receivedAssociationRequest(DicomAssociation association)
        {
            Console.WriteLine(Environment.NewLine);
            if (verbose)
            {
                Console.WriteLine("--------------------------------------------------------------------");
                if (File.Exists("singleImage.txt")) Console.WriteLine("SAVING THUMB.");
                else Console.WriteLine("SAVING SERIES.");
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine("Received association request: ");
                Console.WriteLine("____________________________________________________________________");
                Console.WriteLine(association.ToString());
                Console.WriteLine("____________________________________________________________________");
                Console.WriteLine(Environment.NewLine);
            }
            else
            {
                if (File.Exists("singleImage.txt")) Console.WriteLine("Saving thumb.");
                else Console.WriteLine("Saving series.");
                Console.WriteLine("________________________________________________");
                Console.WriteLine("Received association request from " +
                association.RemoteHost + Environment.NewLine);
            }
        }

        internal static void receivedCStoreRequest(DicomCStoreRequest request)
        {
            // try get useful data
            string instanceNumber = "";
            request.Dataset.TryGetSingleValue<string>(DicomTag.InstanceNumber, out instanceNumber);
            Console.WriteLine("CStore request - image no. " +instanceNumber);
            if (verbose)
            {
                Console.WriteLine("____________________________________________________________________");
                foreach (PropertyInfo propertyInfo in request.GetType().GetProperties())
                    Console.WriteLine(propertyInfo.Name + ":\t " + propertyInfo.GetValue(request));
                Console.WriteLine("____________________________________________________________________");
                Console.WriteLine(Environment.NewLine);
            }
        }

        public static void startedListening(int port)
        {
            Console.WriteLine("Started listening for incoming datasets on port " + port);
        }

        internal static void releaseAssociation()
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("Release association.");
            if (verbose) Console.WriteLine(Environment.NewLine+ "--------------------------------------------------------------------");
            else Console.WriteLine("________________________________________________");
        }

    }

    class EntryPoint
    {
        static void Main(string[] args)
        {
            Console.Title = "Listener";
            int port = int.Parse(args[0]);
            DicomServer.Create<CStoreSCP>(port, File.ReadAllLines("ServerConfig.txt")[8]);
            Debug.startedListening(port);
            while (true) { }
        }
    }
}
