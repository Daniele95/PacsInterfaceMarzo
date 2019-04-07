using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

namespace PacsLibrary
{
    /// <summary>
    /// Client configuration for DICOM data exchange with a server.
    /// </summary>
    [Serializable]
    public class Configuration
    {
        string configurationFile;

        public string host { get; set; } = "dicomserver.co.uk";
        public int port { get; set; } = 11112;
        public string AET { get; set; } = "ANY";

        public bool anonymizeData { get; set; } = true;
        public string thisNodeAET { get; set; } = "ANATOMAGETABLE";
        public int thisNodePort { get; set; } = 104;
        public string fileDestination { get; set; } = "C:/Dicom";

        public bool useTls { get; set; } = false;
        public string certificatePath { get; set; } = "C:/trustStore.p12";
        public string certificatePassword { get; set; } = "password";
        public string keyPath { get; set; } = "C:/keyStore.p12";
        public string keyPassword { get; set; } = "password";

        public Study studyTemplate { get; set; } = new Study {
            new QueryParameter{ name= "StudyInstanceUID",   value="", visible=true },
            new QueryParameter{ name= "PatientName",        value="", visible=true },
            new QueryParameter{ name= "PatientID",          value="", visible=true },
            new QueryParameter{ name= "StudyDate",          value="", visible=true },
            new QueryParameter{ name= "ModalitiesInStudy",  value="", visible=true },
            new QueryParameter{ name= "PatientBirthDate",   value="", visible=true },
            new QueryParameter{ name= "StudyDescription",   value="", visible=true },
            new QueryParameter{ name= "AccessionNumber",    value="", visible=true },
        };
        public Series seriesTemplate { get; set; } = new Series {
            new QueryParameter{ name= "SeriesDescription",  value="", visible=true },
            new QueryParameter{ name= "StudyDate",          value="", visible=true },
            new QueryParameter{ name= "Modality",           value="", visible=true },
            new QueryParameter{ name= "SeriesInstanceUID",  value="", visible=true },
            new QueryParameter{ name= "StudyInstanceUID",   value="", visible=true },
        };

        /// <summary>
        /// Basic Configuration with only server info, the default port to receive data is 104.
        /// </summary>
        /// <param name="host">The IP address of the server to query.</param>
        /// <param name="port">The port to query the server on.</param>
        /// <param name="AET">The DICOM Application Entity Title identifying the server.</param>
        public Configuration(string host, int port, string AET)
        {
            this.host = host; this.port = port; this.AET = AET;
        }

        /// <summary>
        /// Full Configuration without TLS data encryption.
        /// </summary>
        /// <param name="host">The IP address of the server to query.</param>
        /// <param name="port">The port to query the server on.</param>
        /// <param name="AET">The DICOM Application Entity Title identifying the server.</param>
        /// <param name="anonymizeData">Delete sensible patient info from all the data downloaded from the PACS.</param>
        /// <param name="thisNodeAET">Application Entity Title with which the Server identifies the Client (this machine).</param>
        /// <param name="thisNodePort">Port of the Client where the Server will send data.</param>
        /// <param name="fileDestination">Destination where DICOM data incoming from the server will be stored on this machine.</param>
        public Configuration(string host, int port, string AET, bool anonymizeData, string thisNodeAET, int thisNodePort, string fileDestination)
        {
            this.host = host; this.port = port; this.AET = AET;
            this.anonymizeData = anonymizeData; this.thisNodeAET = thisNodeAET;
            this.thisNodePort = thisNodePort; this.fileDestination = fileDestination;
        }

        /// <summary>
        /// Full Configuration with TLS data encryption.
        /// </summary>
        /// <param name="host">The IP address of the server to query.</param>
        /// <param name="port">The port to query the server on.</param>
        /// <param name="AET">The DICOM Application Entity Title identifying the server.</param>
        /// <param name="anonymizeData">Delete sensible patient info from all the data downloaded from the PACS.</param>
        /// <param name="thisNodeAET">Application Entity Title with which the Server identifies the Client (this machine).</param>
        /// <param name="thisNodePort">Port of the Client where the Server will send data.</param>
        /// <param name="fileDestination">Destination where DICOM data incoming from the server will be stored on this machine.</param>
        /// <param name="certificatePath">Archive .p12 of certificates (Trust Store) released by a Certificate Authority for this Server - Client data exchange. It's usually a self-signed certificate (the exchange takes place in a LAN) which can be generated with the Java utility Keytool. It's used by the peer receiving the query to authenticate the incoming data stream.</param>
        /// <param name="certificatePassword">Password of the Trust Store.</param>
        /// <param name="keyPath">Archive .p12 containing peer identities and private keys (Key Store) used to authenticate the data exchange. It's sent from the peer initiating the query to the peer receiving the query.</param>
        /// <param name="keyPassword">Password of the Key Store.</param>
        public Configuration(string host, int port, string AET, bool anonymizeData, string thisNodeAET, int thisNodePort, string fileDestination, string certificatePath, string certificatePassword, string keyPath, string keyPassword)
        {
            this.host = host; this.port = port; this.AET = AET;
            this.anonymizeData = anonymizeData; this.thisNodeAET = thisNodeAET;
            this.thisNodePort = thisNodePort; this.fileDestination = fileDestination;
            this.certificatePath = certificatePath; this.certificatePassword = certificatePassword;
            this.keyPath = keyPath; this.keyPassword = keyPassword;
        }

        /// <summary>
        /// Deserializes the Client Configuration from a .txt file. If the file is empty, serializes a default Configuration.
        /// </summary>
        /// <param name="configurationFile">File containing serialized Configuration.</param>
        public Configuration(string configurationFile)
        {
            this.configurationFile = configurationFile;
            update();
        }

        [field: NonSerialized]
        IFormatter formatter = new BinaryFormatter();

        void update()
        {
            if (!File.Exists(configurationFile)) write();
            var stream = new FileStream(configurationFile, FileMode.Open, FileAccess.Read);
            var newConfig = (Configuration)formatter.Deserialize(stream);
            foreach (var property in this.GetType().GetProperties())
                property.SetValue(this, newConfig.GetType().GetProperty(property.Name).GetValue(newConfig));
            stream.Close();
        }

        /// <summary>
        /// Serializes Configuration on the .txt file specified in the constructor.
        /// </summary>
        public void write()
        {
            Stream stream = new FileStream(configurationFile, FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, this);
            stream.Close();
        }

    }
}

