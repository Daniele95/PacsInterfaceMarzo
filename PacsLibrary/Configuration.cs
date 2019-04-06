using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace PacsLibrary
{
    [Serializable]
    public class Configuration
    {
        string configurationFile;

        public string host { get; set; }
        public int port { get; set; }
        public string AET { get; set; }

        public bool anonymizeData { get; set; } = true;
        public string thisNodeAET { get; set; } = "ANATOMAGETABLE";
        public int thisNodePort { get; set; } = 104;
        public string fileDestination { get; set; } = "C:/Dicom";
        public bool useTls { get; set; } = false;

        public string keyStoreName { get; set; }
        public string trustStorePath { get; set; }
        public string trustStorePassword { get; set; }

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
            this.keyStoreName = keyStoreName; this.trustStorePath = trustStorePath; this.trustStorePassword = trustStorePassword;
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

