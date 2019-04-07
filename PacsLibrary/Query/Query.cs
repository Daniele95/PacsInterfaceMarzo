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
        /// <summary>
        /// Queries the server indicated in <see cref="Configuration"/>
        /// with the query parameters indicated in <see cref="Study"/>. 
        /// Implements the DICOM C-FIND command at STUDY Level.
        /// </summary>
        /// <param name="configuration">Server and client configuration.</param>
        /// <param name="studyQuery">Parameters specifying the query.</param>
        public static List<Study> CFINDStudies(Configuration configuration,Study studyQuery)
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
                var _networkStream = new DesktopNetworkStream(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            return studyResponses;

        }

        /// <summary>
        /// Queries the server indicated in <see cref="Configuration"/>
        /// for the various Series contained 
        /// in the specified <see cref="Study"/>. 
        /// Implements the DICOM C-FIND command at SERIES Level.
        /// </summary>
        /// <param name="configuration">Server and client configuration.</param>
        /// <param name="studyQuery">Parameters specifying the query.</param>
        public static List<Series> CFINDSeries(Configuration configuration,Study studyResponse)
        {
            var seriesParametersToShow = configuration.seriesTemplate;
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
                var _networkStream = new DesktopNetworkStream(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer(); }

            // arrange results in table
            return seriesResponses;

        }

        /// <summary>
        /// Asks the server indicated in <see cref="Configuration"/> 
        /// to send the specified <see cref="Series"/>.
        /// Implements the DICOM C-MOVE Command at SERIES level.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <returns>The path where the series has been saved.</returns>
        public static string CMOVESeries(Configuration configuration, Series seriesResponse)
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
                var _networkStream = new DesktopNetworkStream(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
                Debug.done();
            }
            else Console.WriteLine("File is already present in database.");
            return path;
        }

        /// <summary>
        /// Downloads an image representative of a given <see cref="Series"/>. 
        /// In this case, the image in the middle of the Series.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <returns>The downloaded image as a BitmapImage.</returns>
        public static BitmapImage downloadSampleImage (Configuration configuration,Series seriesResponse)
        {
            BitmapImage img = new BitmapImage();
            try
            {
                string SOPInstanceUID = CFINDImagesInSeries(configuration, seriesResponse);
                if (SOPInstanceUID != "")
                {
                    Debug.downloadingImage(configuration, SOPInstanceUID);
                    img = CMOVEImage(configuration, seriesResponse, SOPInstanceUID);
                    Debug.done();
                }

            }
            catch (Exception ec) { Console.WriteLine(ec.StackTrace); }
            return img;
        }

        /// <summary>
        /// Finds the image in the middle of a given <see cref="Series"/>.
        /// Implements the DICOM C-FIND command at IMAGE level.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <returns>The SOPInstanceUID of the image representative of the series.</returns>
        static string CFINDImagesInSeries(Configuration configuration,Series seriesResponse)
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
                var _networkStream = new DesktopNetworkStream(configuration, true, true);
                client.Send(_networkStream, configuration.thisNodeAET, configuration.AET, 5000);
            }
            catch (Exception) { Debug.cantReachServer();Console.WriteLine("sono qui"); }

            // get just the image at the half of the series
            string SOPInstanceUID = "";
            if (imageIDs.Count > 0) SOPInstanceUID = imageIDs[(int)(imageIDs.Count / 2.0f)];
            return SOPInstanceUID;

        }

        /// <summary>
        /// Asks the server indicated in <see cref="Configuration"/> to send the image specified
        /// in the <see cref="string"/>, representative of the <see cref="Series"/>.
        /// Implements DICOM C-MOVE Command at IMAGE level.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <param name="SOPInstanceUID">SOPInstanceUID specifying the query.</param>
        /// <returns>The .dcm image representative of the series as a BitmapImage.</returns>
        static BitmapImage CMOVEImage(Configuration configuration,Series seriesResponse, string SOPInstanceUID)
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
            var _networkStream = new DesktopNetworkStream(configuration, true, true);
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
