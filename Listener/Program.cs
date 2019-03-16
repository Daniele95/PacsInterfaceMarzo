using Dicom;
using Dicom.Imaging;
using Dicom.Network;
using LiteDB;
using System;
using System.Globalization;
using System.IO;

namespace Listener
{
    class Program
    {
        internal static DicomCStoreResponse onCStoreRequest(DicomCStoreRequest request)
        {
            Console.WriteLine("received cstore request " + request.ToString());

            if (File.Exists("singleImage.txt"))
                saveImage(request);
            else
            {
                Console.WriteLine("now save in database" + Environment.NewLine);
                try { saveSeries(request); }
                catch (Exception ec) { Console.WriteLine(ec.Message + "   " + ec.ToString()); }
            }
            return new DicomCStoreResponse(request, DicomStatus.Success);

        }

        private static void saveImage(DicomCStoreRequest request)
        {
            Console.WriteLine("now save on ./images/file.jpg");

            File.Delete("./images/file.dcm");
            var dicomFile = new DicomFile(request.File.Dataset);
            dicomFile.Save("./images/file.dcm");
            var image = new DicomImage("./images/file.dcm");
            File.Delete("./images/file.jpg");

            image.RenderImage().AsClonedBitmap().Save("./images/file.jpg");
        }

        private static void saveSeries(DicomCStoreRequest request)
        { 
            // get parameters
            string dateString = "";
            request.Dataset.TryGetSingleValue(DicomTag.StudyDate, out dateString);
            DateTime date = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
            string StudyInstanceUID = "";
            request.Dataset.TryGetSingleValue(DicomTag.StudyInstanceUID, out StudyInstanceUID);
            string SeriesInstanceUID = "";
            request.Dataset.TryGetSingleValue(DicomTag.SeriesInstanceUID, out SeriesInstanceUID);
            string SOPInstanceUID = "";
            request.Dataset.TryGetSingleValue(DicomTag.SOPInstanceUID, out SOPInstanceUID);

            // save in database folder
            var pathInDatabase = Path.GetFullPath("./databaseFolder");
            // take last two numbers, for examples seriesinstanceuid ends with .150.0
            var seriesUIDs = SeriesInstanceUID.Split('.');
            string seriesUID = seriesUIDs[seriesUIDs.Length - 2] + "." + seriesUIDs[seriesUIDs.Length - 1];
            var sopUIDs = SOPInstanceUID.Split('.');
            string sopUID = sopUIDs[sopUIDs.Length - 2] + "." + sopUIDs[sopUIDs.Length - 1];

            pathInDatabase = Path.Combine(pathInDatabase, date.Year.ToString(), date.Month.ToString(), date.Day.ToString(),
                StudyInstanceUID, seriesUID);
            if (!Directory.Exists(pathInDatabase)) Directory.CreateDirectory(pathInDatabase);
            string imagePath = Path.Combine(pathInDatabase,
                sopUID + ".dcm");

            if (!File.Exists(imagePath))
            {
                // anonymize
                if (bool.Parse(File.ReadAllLines("ServerConfig.txt")[3]))
                {
                    var profile = new DicomAnonymizer.SecurityProfile();
                    profile.PatientName = "random";
                    profile.PatientID = "random";
                    DicomAnonymizer anonymizer = new DicomAnonymizer(profile);
                    request.Dataset = anonymizer.Anonymize(request.Dataset);
                }
                // get more data
                string PatientName = "";
                request.Dataset.TryGetSingleValue(DicomTag.PatientName, out PatientName);
                Console.WriteLine("patient name " + PatientName);
                string PatientID = "";
                request.Dataset.TryGetSingleValue(DicomTag.PatientID, out PatientID);

                // add entry in database
                var study = new Study
                {
                    StudyInstanceUID = StudyInstanceUID,
                    PatientID = PatientID,
                    PatientName = PatientName,
                    StudyDate = date.ToString()
                };
                using (var db = new LiteDatabase("./databaseFolder/database.db"))
                {
                    var studies = db.GetCollection<Study>("studies");
                    if (studies.FindById(StudyInstanceUID) == null) studies.Insert(study);
                }
                //

                request.File.Save(imagePath);
                Console.WriteLine("received and saved a file in database");
            }
            else Console.WriteLine("File already present in database");
        }

    }
    public class Study
    {
        [BsonId]
        public string StudyInstanceUID { get; set; } = "";
        public string PatientID { get; set; } = "";
        public string PatientName { get; set; } = "";
        public string StudyDate { get; set; } = "";
    }
}
