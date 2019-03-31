using Dicom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;

namespace PacsInterface
{
    public class CurrentConfiguration
    {
        public string ip { get; private set; }
        public int port { get; private set; }
        public string AET { get; private set; }
        public bool anonymizeData { get; private set; }
        public string thisNodeAET { get; private set; }
        public int thisNodePort { get; private set; }
        public string fileDestination { get; private set; }
        public bool useTls { get; private set; }
        public string keyStoreName { get; private set; }
        public string trustStorePath { get; private set; }
        public string trustStorePassword { get; private set; }


        public CurrentConfiguration()
        {
            update();
            restartListener();
            startConfigurationWatcher();
        }

        public void update()
        {
            bool locked = true;
            while (locked) locked = IsFileLocked(new FileInfo("ServerConfig.txt"));
            if (!locked)
            {
                var lines = File.ReadAllLines("ServerConfig.txt");
                var file = File.OpenRead("ServerConfig.txt");
                ip = lines[0];
                port = int.Parse(lines[1]);
                AET = lines[2];
                anonymizeData = bool.Parse(lines[3]);
                thisNodeAET = lines[4];
                thisNodePort = int.Parse(lines[5]);
                fileDestination = lines[6];
                useTls = bool.Parse(lines[7]);
                keyStoreName = lines[8];
                trustStorePath = lines[9];
                trustStorePassword = lines[10];
                file.Close();
            }
        }
        void restartListener()
        {
            Process[] listeners = Process.GetProcessesByName("Listener");
            if (listeners.Length != 0) foreach (var listener in listeners) listener.Kill();
            var newListener = new Process
            {
                StartInfo = {
                    FileName = "Listener",
                    Arguments = thisNodePort.ToString()+" "+useTls.ToString()
                }
            };
            newListener.Start();
        }
        void startConfigurationWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName("./"),
                Filter = Path.GetFileName("ServerConfig.txt"),
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += new FileSystemEventHandler((o, e) =>
            {
                int oldPort = thisNodePort;
                bool oldAnonymizeData = anonymizeData;
                update();
                if (thisNodePort != oldPort || anonymizeData != oldAnonymizeData) restartListener();

            });
        }
        public static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try { stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None); }
            catch (IOException) { return true; }
            finally { if (stream != null) stream.Close(); }
            return false;
        }

    }

    public class QueryParameter
    {
        public string name { get; set; } = "";
        public string value { get; set; } = "";
        public bool visible { get; set; } = true;
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
                        if (value.Length > 0)
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
        protected string StudyDate = "";

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

        internal string getFullPath(string fileDestination)
        {
            return Path.Combine(
                fileDestination,
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString(),
                DateTime.Now.Day.ToString(),
                this.getStudyInstanceUID(),
                this.getSeriesInstanceUID());
        }

        public string getStudyDate()
        {
            if (StudyDate == "")
                foreach (var queryParameter in this)
                    if (queryParameter.name == "StudyDate")
                    {
                        var date = DateTime.ParseExact(queryParameter.value, "yyyyMMdd",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        StudyDate = date.ToString("yyyy/MM/dd");
                    }
            return StudyDate;
        }
    }

    public class Debug
    {
        static void line()
        {
            Console.WriteLine("-------------------------------------------------"
                + Environment.NewLine);
        }

        static void breakLine()
        {
            Console.Write(Environment.NewLine+Environment.NewLine);
        }

        internal static void welcome()
        {
            Console.Write("Welcome to the Pacs Interface of the Anatomage Table!");
            breakLine();
        }
        internal static void gotNumberOfResults(int count)
        {
            Console.Write("got " + count.ToString()); breakLine();
            breakLine();
        }
        internal static void studyQuery(CurrentConfiguration configuration, Study studyQuery)
        {
            Console.Write("Querying server " + configuration.ip + ":" + configuration.port
                + " for STUDIES");
            breakLine();
        }
        internal static void seriesQuery(CurrentConfiguration configuration, Series seriesTemplate)
        {
            line();
            Console.Write( "Querying server " + configuration.ip + ":" + configuration.port
                + " for SERIES in study no. " + seriesTemplate.getStudyInstanceUID());
            breakLine();
        }
        internal static void cantReachServer()
        {
            Console.Write("Impossible to reach the server");
            breakLine();
        }

        internal static void downloading(CurrentConfiguration configuration)
        {
            Console.Write("Downloading series from server " + configuration.ip + ":" + configuration.port);
            breakLine();
        }
        internal static void done()
        {
            Console.Write("Done."); breakLine();
        }
        internal static void downloadingImage(CurrentConfiguration configuration, string SOPInstanceUID)
        {
            Console.Write("Downloading from server "
                + configuration.ip + ":" + configuration.port
                + " sample image no. " + SOPInstanceUID);
            breakLine();
        }
        internal static void imageQuery(CurrentConfiguration configuration, Series seriesResponse)
        {
            Console.Write( "Querying server " + configuration.ip + ":" + configuration.port +
                       " for IMAGES in series no. " + seriesResponse.getSeriesInstanceUID());
            breakLine();
        }
    }
}
