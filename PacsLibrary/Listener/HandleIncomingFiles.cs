using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using Dicom.Network;
using System;
using System.IO;
using System.Threading;

namespace PacsLibrary.Listener
{
    /// <summary>
    /// Implements the main methods used by the CStoreSCP Listener to handle incoming DICOM files.
    /// </summary>
    class HandleIncomingFiles
    {

        internal static DicomCStoreResponse onCStoreRequest(DicomCStoreRequest request)
        {
            bool singleImage = File.Exists("singleImage.txt");
            Debug.receivedCStoreRequest(request);

            if (singleImage)
                saveThumb(request);
            else
            {
                try { saveSeries(request); }
                catch (Exception ec) { Console.WriteLine(ec.Message + "   " + ec.ToString()); }
            }
            return new DicomCStoreResponse(request, DicomStatus.Success);

        }

        private static void saveThumb(DicomCStoreRequest request)
        {
            File.Delete("./images/file.dcm");
            var dicomFile = new DicomFile(request.File.Dataset);
            dicomFile.Save("./images/file.dcm");
            var image = new DicomImage("./images/file.dcm");
            File.Delete("./images/file.jpg");

            image.RenderImage().AsClonedBitmap().Save("./images/file.jpg");
        }

        private static void saveSeries(DicomCStoreRequest request)
        {
            string path = File.ReadAllLines("pathForDownload.txt")[0];

            // get parameters
            string SOPInstanceUID = "";
            request.Dataset.TryGetSingleValue(DicomTag.SOPInstanceUID, out SOPInstanceUID);

            var configuration = new Configuration("ServerConfig.txt");

            // Anonymize all files
            if (configuration.anonymizeData)
            {
                request.Dataset = request.Dataset.AddOrUpdate(DicomTag.PatientName,"random");
                request.Dataset = request.Dataset.AddOrUpdate(DicomTag.PatientID, "random");
                request.Dataset = request.Dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now);
            }
            
            // save
            string filePath = Path.Combine(path, SOPInstanceUID + ".dcm");
            request.File.Save(filePath);

            updateDatabase();
        }

        private static void updateDatabase()
        {
            var configuration = new Configuration("ServerConfig.txt");
            string fileDestination = configuration.fileDestination;
            string dicomDirPath = Path.Combine(fileDestination, "DICOMDIR");

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = new DicomDirectory();
                int i = 0;
                foreach (string file in Directory.EnumerateFiles(fileDestination, "*.dcm*", SearchOption.AllDirectories))
                {
                    var dicomFile = DicomFile.Open(file);
                    dicomDir.AddFile(dicomFile, i.ToString()); i++;
                }
                dicomDir.Save(fileStream);
            }
        }

    }
}
