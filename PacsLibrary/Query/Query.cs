using Dicom;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PacsLibrary.Query
{
    public class Query
    {
        public static Study studyParametersToShow(string studyParametersToShow)
        {
            var studyTemplate = new Study();
            bool studyInstanceUIDVisible = false;
            foreach (var line in File.ReadAllLines("StudyColumnsToShow.txt"))
            {
                if (line != "StudyInstanceUID")
                    studyTemplate.Add(new QueryParameter { name = line });
                else
                    studyInstanceUIDVisible = true;
            }
            studyTemplate.Add(new QueryParameter { name = "StudyInstanceUID", visible = studyInstanceUIDVisible });
            return studyTemplate;
        }

        public static List<Study> searchStudies(CurrentConfiguration configuration,Study studyQuery)
        {
            var studyResponses = new List<Study>();
            // init find request
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);

            foreach (QueryParameter studyParameter in studyQuery)
                cfind.Dataset.Add(studyParameter.getTag(), studyParameter.value);

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) studyResponses.Add(new Study(response.Dataset, studyQuery));
                if (!response.HasDataset) Debug.gotNumberOfResults(studyResponses.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            studyResponses = new List<Study>();

            // send query
            Debug.studyQuery(configuration, studyQuery);
            try
            {
                var _networkStream = new DesktopNetworkStreamTls(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            return studyResponses;

        }

        public static Series seriesParametersToShow(string seriesParametersToShow)
        {
            Series seriesTemplate = new Series();
            bool studyInstanceUIDVisible = false;
            bool seriesInstanceUIDVisible = false;
            foreach (var line in File.ReadAllLines(seriesParametersToShow))
            {
                if (line == "StudyInstanceUID") studyInstanceUIDVisible = true;
                else if (line == "SeriesInstanceUID") seriesInstanceUIDVisible = true;
                else seriesTemplate.Add(new QueryParameter { name = line });
            }
            seriesTemplate.Add(new QueryParameter { name = "StudyInstanceUID", visible = studyInstanceUIDVisible });
            seriesTemplate.Add(new QueryParameter { name = "SeriesInstanceUID", visible = seriesInstanceUIDVisible });
            return seriesTemplate;
        }

        public static List<Series> searchSeries(CurrentConfiguration configuration,Study studyResponse, string _seriesParametersToShow)
        {
            var seriesParametersToShow = Query.seriesParametersToShow(_seriesParametersToShow);
            var seriesResponses = new List<Series>();
            // init find request
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Series);
            seriesParametersToShow.setStudyInstanceUID(studyResponse);

            foreach (QueryParameter studyParameter in seriesParametersToShow)
                cfind.Dataset.Add(studyParameter.getTag(), studyParameter.value);

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) seriesResponses.Add(new Series(response.Dataset, seriesParametersToShow));
                else Debug.gotNumberOfResults(seriesResponses.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            seriesResponses = new List<Series>();

            // send query
            Debug.seriesQuery(configuration, seriesParametersToShow);
            try
            {
                var _networkStream = new DesktopNetworkStreamTls(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            return seriesResponses;

        }


        public static void downloadSeries(CurrentConfiguration configuration, Series seriesResponse)
        {
            // init move request
            var cmove = new DicomCMoveRequest(configuration.thisNodeAET, seriesResponse.getStudyInstanceUID(), seriesResponse.getSeriesInstanceUID());

            var client = new DicomClient();
            client.AddRequest(cmove);

            // prepare to receive data
            File.Delete("singleImage.txt");
            // write file path based on uids, current date, and path chosen by user
            string path = seriesResponse.getFullPath(configuration.fileDestination);
            Directory.CreateDirectory(path);
            using (var streamWriter = new StreamWriter("pathForDownload.txt", false))
            {
                streamWriter.WriteLine(path);
            }

            // send query
            if (Directory.GetFiles(path).Length == 0)
            {
                Debug.downloading(configuration);
                var _networkStream = new DesktopNetworkStreamTls(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
                Debug.done();
            }
            else Console.WriteLine("File is already present in database.");

        }


        public static BitmapImage downloadSampleImage (CurrentConfiguration configuration,Series seriesResponse)
        {
            BitmapImage img = new BitmapImage();
            try
            {
                string SOPInstanceUID = getImagesInSeries(configuration, seriesResponse);
                if (SOPInstanceUID != "")
                {
                    Debug.downloadingImage(configuration, SOPInstanceUID);
                    img = downloadImage(configuration, seriesResponse, SOPInstanceUID);
                    Debug.done();
                }

            }
            catch (Exception ec) { Console.WriteLine(ec.StackTrace); }
            return img;
        }
        static string getImagesInSeries(CurrentConfiguration configuration,Series seriesResponse)
        {
            var imageIDs = new List<string>();

            // init image query
            DicomCFindRequest cfind = new DicomCFindRequest(DicomQueryRetrieveLevel.Image);

            cfind.Dataset.Add(DicomTag.StudyInstanceUID, seriesResponse.getStudyInstanceUID());
            cfind.Dataset.Add(DicomTag.SeriesInstanceUID, seriesResponse.getSeriesInstanceUID());
            cfind.Dataset.Add(DicomTag.SOPInstanceUID, "");
            cfind.Dataset.Add(DicomTag.InstanceNumber, "");

            cfind.OnResponseReceived = (request, response) =>
            {
                if (response.HasDataset) imageIDs.Add(response.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                if (!response.HasDataset) Debug.gotNumberOfResults(imageIDs.Count);
            };

            var client = new DicomClient();
            client.AddRequest(cfind);

            // prepare to receive data
            imageIDs = new List<string>();

            // send query
            Debug.imageQuery(configuration, seriesResponse);
            try
            {
                var _networkStream = new DesktopNetworkStreamTls(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer();Console.WriteLine("sono qui"); }

            // get just the image at the half of the series
            string SOPInstanceUID = "";
            if (imageIDs.Count > 0) SOPInstanceUID = imageIDs[(int)(imageIDs.Count / 2.0f)];
            return SOPInstanceUID;

        }
        static BitmapImage downloadImage(CurrentConfiguration configuration,Series seriesResponse, string SOPInstanceUID)
        {
            // init move request
            var cmove = new DicomCMoveRequest(configuration.thisNodeAET, seriesResponse.getStudyInstanceUID(), seriesResponse.getSeriesInstanceUID(), SOPInstanceUID);

            var client = new DicomClient();
            client.AddRequest(cmove);

            // prepare to receive data
            File.Create("singleImage.txt").Close();
            DirectoryInfo di = new DirectoryInfo("./images/");
            di.Create();
            foreach (FileInfo file in di.GetFiles()) file.Delete();

            // send query
            var _networkStream = new DesktopNetworkStreamTls(configuration, true, true);
            client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);


            // load the image downloaded from the listener
            var image = new BitmapImage();
            if (File.Exists("./images/file.jpg"))
            {
                var uriSource = new Uri(Path.GetFullPath("./images/file.jpg"));
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = uriSource;
                image.EndInit();
            }
            else Console.WriteLine("Listener didn't receive anything or didn't work");

            File.Delete("singleImage.txt");
            return image;
        }

    }
}
