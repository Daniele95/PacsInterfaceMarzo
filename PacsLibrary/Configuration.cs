using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

namespace PacsLibrary
{
    [Serializable]
    public class Configuration
    {
        string configurationFile;

        public string host { get; set; } = "dicomserver.co.uk";
        public int port { get; set; } = 104;
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


        public Configuration(string host, int port, string AET)
        {
            this.host = host; this.port = port; this.AET = AET;
        }

        public Configuration(string host, int port, string AET, bool anonymizeData, string thisNodeAET, int thisNodePort, string fileDestination, bool useTls)
        {
            this.host = host; this.port = port; this.AET = AET;
            this.anonymizeData = anonymizeData; this.thisNodeAET = thisNodeAET;
            this.thisNodePort = thisNodePort; this.fileDestination = fileDestination; this.useTls = useTls;
        }

        public Configuration(string host, int port, string AET, bool anonymizeData, string thisNodeAET, int thisNodePort, string fileDestination, string keyStoreName, string trustStorePath, string trustStorePassword)
        {
            this.host = host; this.port = port; this.AET = AET;
            this.anonymizeData = anonymizeData; this.thisNodeAET = thisNodeAET;
            this.thisNodePort = thisNodePort; this.fileDestination = fileDestination;
            this.certificatePath = keyStoreName; this.keyPath = trustStorePath; this.keyPassword = trustStorePassword;
        }

        public Configuration(string configurationFile)
        {
            this.configurationFile = configurationFile;
            update();
        }

        [field: NonSerialized]
        IFormatter formatter = new BinaryFormatter();

        public void update()
        {
            if (!File.Exists(configurationFile)) write();
            var stream = new FileStream(configurationFile, FileMode.Open, FileAccess.Read);
            var newConfig = (Configuration)formatter.Deserialize(stream);
            foreach (var property in this.GetType().GetProperties())
                property.SetValue(this, newConfig.GetType().GetProperty(property.Name).GetValue(newConfig));
            stream.Close();
        }

        public void write()
        {
            Stream stream = new FileStream(configurationFile, FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, this);
            stream.Close();
        }

    }
}

