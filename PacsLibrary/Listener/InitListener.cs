using Dicom.Network;
using System;
using System.Security.Cryptography.X509Certificates;

namespace PacsLibrary.Listener
{
    public class Listener
    {
        public static void init(Configuration configuration)
        {
            Console.Title = "Listener";
            string certificateName = addTlsCertificate(configuration.certificatePath, configuration.certificatePassword);
            DicomServer.Create<CStoreSCP>(configuration.thisNodePort, certificateName);
            Debug.startedListening(configuration.thisNodePort);
            while (true) { }
        }

        static string addTlsCertificate(string certificatePath, string certificatePassword)
        {
            string certificateName = "";
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2 newCert = new X509Certificate2(certificatePath, certificatePassword);
                store.Add(newCert);
                certificateName = newCert.GetNameInfo(X509NameType.SimpleName, false).Split(' ')[0];
            }
            catch (Exception) { }

            return certificateName;
        }
    }
}
