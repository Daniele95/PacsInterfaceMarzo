using Dicom;
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
            Console.WriteLine("received cstore request " + request.ToString());

            if (File.Exists("singleImage.txt"))
                saveThumb(request);
            else
            {
                Console.WriteLine("now save in database" + Environment.NewLine);
                try { saveSeries(request); }
                catch (Exception ec) { Console.WriteLine(ec.Message + "   " + ec.ToString()); }
            }
            return new DicomCStoreResponse(request, DicomStatus.Success);

        }

        private static void saveSeries(DicomCStoreRequest request)
        {
            string path = File.ReadAllLines("pathForDownload.txt")[0];
            // if path not empty:
            if (Directory.GetFiles(path).Length == 0)
            {
                // get parameters
                string imageID = "";
                request.Dataset.TryGetSingleValue(DicomTag.ImageID, out imageID);

                // Anonymize all files
                if (bool.Parse(File.ReadAllLines("ServerConfig.txt")[3]))
                {
                    var ad = DicomAnonymizer.SecurityProfileOptions.BasicProfile;
                    var profile = new DicomAnonymizer.SecurityProfile();
                    profile.PatientName = "random";
                    profile.PatientID = "random";
                    DicomAnonymizer anonymizer = new DicomAnonymizer(profile);
                    request.Dataset = anonymizer.Anonymize(request.Dataset);
                }

                // save
                string filePath = Path.Combine(path, imageID + ".dcm");
                request.File.Save(filePath);
               // addEntryInDatabase(filePath);
                Console.WriteLine("received and saved a file in database");
            }
            else Console.WriteLine("File already present in database");
            updateDatabase();
        }

        private static void saveThumb(DicomCStoreRequest request)
        {
            Console.WriteLine("now save on ./images/file.jpg");

            File.Delete("./images/file.dcm");
            var dicomFile = new DicomFile(request.File.Dataset);
            dicomFile.Save("./images/file.dcm");
            var image = new DicomImage("./images/file.dcm");
            File.Delete("./images/file.jpg");

            image.RenderImage().AsClonedBitmap().Save("./images/file.jpg");
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
                    Console.WriteLine("save file: " + dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName));
                }
                dicomDir.Save(fileStream);
            }
        }

        /*private static void addEntryInDatabase(string filePath)
        {
            string fileDestination = File.ReadAllLines("ServerConfig.txt")[6];
            string dicomDirPath = Path.Combine(fileDestination, "DICOMDIR");

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir == null)
                {
                    Console.WriteLine("NULLO!!!");
                    dicomDir = new DicomDirectory();

                }
                var dicomFile = DicomFile.Open(filePath);
                dicomDir.AddFile(dicomFile, DateTime.Now.Second.ToString());
                Console.WriteLine("TEMPO: "+ DateTime.Now.Second.ToString());


                Console.WriteLine("save file: " + dicomFile.Dataset.GetSingleValue<string>(DicomTag.PatientName));
                dicomDir.Save(fileStream);
            }
        }
        */
    }
}
