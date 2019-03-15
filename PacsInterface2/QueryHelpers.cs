using Dicom;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace PacsInterface2
{
    public class CurrentConfiguration
    {
        public string ip { get; private set; }
        public int port { get; private set; }
        public string AET { get; private set; }
        public bool saveData { get; private set; }
        public string thisNodeAET { get; private set; }
        public int thisNodePort { get; private set; }

        public CurrentConfiguration()
        {
            update();
        }

        public void update()
        {
            ip = File.ReadAllLines("ServerConfig.txt")[0];
            port = int.Parse(File.ReadAllLines("ServerConfig.txt")[1]);
            AET = File.ReadAllLines("ServerConfig.txt")[2];
            saveData = bool.Parse(File.ReadAllLines("ServerConfig.txt")[3]);
            thisNodeAET = File.ReadAllLines("ServerConfig.txt")[4];
            thisNodePort = int.Parse(File.ReadAllLines("ServerConfig.txt")[5]);
        }

    }

    public class QueryParameter
    {
        public string name { get; set; } = "";
        public string value { get; set; } = "";
        public DicomTag getTag()
        {
            string tagNumber = typeof(DicomTag).GetField(name).GetValue(null).ToString();
            return DicomTag.Parse(tagNumber);
        }
    }

    public class Study : List<QueryParameter>
    {
        // constructors to fill with query parameters
        public Study() { }
        // constructor from query results
        public Study(DicomDataset dataset, Study studyQuery)
        {
            dataset.TryGetSingleValue(DicomTag.StudyInstanceUID, out StudyInstanceUID);

            foreach (var studyQueryParameter in studyQuery)
            {
                string value = "";
                if (studyQueryParameter.name == "ModalitiesInStudy")
                // get multiple values for ModalitiesInStudy
                {
                    var modalities = new string[10];
                    dataset.TryGetValues(studyQueryParameter.getTag(), out modalities);
                    if (modalities != null)
                    {
                        foreach (var modality in modalities)
                            value = value + modality + ", ";
                        value = value.Substring(0, value.Length - 2);
                    }
                }
                else dataset.TryGetSingleValue(studyQueryParameter.getTag(), out value);
                var studyParameter = new QueryParameter { name = studyQueryParameter.name, value = value };
                this.Add(studyParameter);
            }
        }

        protected string StudyInstanceUID = "";

        public string getStudyInstanceUID()
        {
            if (StudyInstanceUID == "")
            {
                foreach (var queryParameter in this)
                    if (queryParameter.name == "StudyInstanceUID")
                        StudyInstanceUID = queryParameter.value;
            }
            return StudyInstanceUID;
        }
        public dynamic getDynamic()
        {
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var studyParameter in this)
                expando[studyParameter.name] = studyParameter.value;
            return expando as dynamic;
        }
        public void print()
        {
            foreach (var par in this)
                Console.WriteLine(par.name + " " + par.value + Environment.NewLine);
        }
    }

    public class Series : Study
    {
        // constructors to fill with query parameters
        public Series() { }
        // constructor from query results
        public Series(DicomDataset dataset, Series seriesQuery) : base(dataset, seriesQuery) { }

        protected string SeriesInstanceUID = "";

        public string getSeriesInstanceUID()
        {
            if (SeriesInstanceUID == "")
            {
                foreach (var queryParameter in this)
                    if (queryParameter.name == "SeriesInstanceUID")
                        SeriesInstanceUID = queryParameter.value;
            }
            return SeriesInstanceUID;
        }
        public void setStudyInstanceUID(Study incomingStudy)
        {
            StudyInstanceUID = incomingStudy.getStudyInstanceUID();

            foreach (var queryParameter in this)
                if (queryParameter.name == "StudyInstanceUID")
                    queryParameter.value = StudyInstanceUID;
        }
    }

    public class Debug
    {
        internal static void gotNumberOfResults(int count)
        {
            Console.WriteLine("got " + count.ToString() + " studies" + Environment.NewLine);
        }
        internal static void studyQuery(CurrentConfiguration configuration, Study studyQuery)
        {
            Console.WriteLine("Querying server " + configuration.ip + ":" + configuration.port
                + " for STUDIES with:" + Environment.NewLine + studyQuery.ToString());
        }
        internal static void seriesQuery(CurrentConfiguration configuration, Series seriesTemplate)
        {
            Console.WriteLine(Environment.NewLine
                + "#############################################################"
                + Environment.NewLine + Environment.NewLine
                + "Querying server " + configuration.ip + ":" + configuration.port
                + " for SERIES in study no. " + seriesTemplate.getStudyInstanceUID());
        }
        internal static void cantReachServer()
        {
            Console.WriteLine("Impossible to reach the server");
        }
        internal static void addedSeriesToTable()
        {
            Console.WriteLine("Added series to table"
                + Environment.NewLine
                + "-------------------------------------------------------------------"
                + Environment.NewLine);
        }
        internal static void downloading(CurrentConfiguration configuration)
        {
            Console.WriteLine("Downloading series from server " + configuration.ip + ":" + configuration.port);
        }
        internal static void done()
        {
            Console.WriteLine("Done.");
        }
        internal static void downloadingImage(CurrentConfiguration configuration, string SOPInstanceUID)
        {
            Console.WriteLine("Downloading from server "
                + configuration.ip + ":" + configuration.port
                + " sample image no. " + SOPInstanceUID + Environment.NewLine);
        }
        internal static void imageQuery(CurrentConfiguration configuration, Series seriesResponse)
        {
            Console.WriteLine(Environment.NewLine + "Querying server " + configuration.ip + ":" + configuration.port +
                       " for IMAGES in series no. " + seriesResponse.getSeriesInstanceUID());
        }
    }
}
