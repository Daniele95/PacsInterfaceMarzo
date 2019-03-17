﻿using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using Dicom.Network;
using System;
using System.IO;
using System.Threading;

namespace Listener
{
    class Program
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
            string imageID = "";
            request.Dataset.TryGetSingleValue(DicomTag.ImageID, out imageID);

            // Anonymize all files
            if (bool.Parse(File.ReadAllLines("ServerConfig.txt")[3]))
            {
                var profile = new DicomAnonymizer.SecurityProfile();
                profile.PatientName = "random";
                profile.PatientID = "random";
                DicomAnonymizer anonymizer = new DicomAnonymizer(profile);
                request.Dataset = anonymizer.Anonymize(request.Dataset);
            }

            // save
            string filePath = Path.Combine(path, imageID + ".dcm");
            request.File.Save(filePath);

            updateDatabase();
        }

        private static void updateDatabase()
        {
            string fileDestination = File.ReadAllLines("ServerConfig.txt")[6];
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
