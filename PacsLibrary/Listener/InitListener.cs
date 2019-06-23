using Dicom.Network;
using System;
using System.Security.Cryptography.X509Certificates;
using PacsLibrary.Configurator;

namespace PacsLibrary.Listener
{
    public class Listener
    {
        /// <summary>
        /// Initializes a TcpListener for incoming DICOM datasets on the local port specified in the 
        /// <see cref="Configuration"/>. If TLS authentication is enabled in <see cref="Configuration"/>,
        /// the TcpListener is started with a Certificate which will be
        /// used to authenticate the data stream incoming from the server.
        /// This requires administrator privileges.
        /// </summary>
        /// <param name="configuration"></param>
        public Listener() { init(); }

        public static void init()
        {
            Configuration configuration = new Configuration();
            Console.Title = "Listener";
            string certificateName = "";
            if(configuration.useTls)
                certificateName = addTLSCertificate(configuration.keyStorePath, configuration.keyStorePassword);
            DicomServer.Create<CStoreSCP>(configuration.thisNodePort, certificateName);
            Debug.startedListening(configuration.thisNodePort);
            while (true) { }
        }

        /// <summary>
        /// Sets the given certificate in the "Local Machine" Certificate StoreLocation.
        /// Requires administrator privileges. Returns the name which identifies the certificate
        /// in the "Local Machine" Certificate StoreLocation.
        /// </summary>
        /// <param name="certificatePath">Path of the .p12 certificate, either released from a Certificate Authority
        /// or self-signed.</param>
        /// <param name="certificatePassword"></param>
        /// <returns>Name which identifies the certificate in the "Local Machine" Certificate StoreLocation</returns>
        static string addTLSCertificate(string certificatePath, string certificatePassword)
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
