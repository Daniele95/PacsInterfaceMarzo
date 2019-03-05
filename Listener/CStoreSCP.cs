using Dicom;
using Dicom.Imaging;
using Dicom.Log;
using Dicom.Network;
using LiteDB;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Listener
{
    public class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
                                                                            {
                                                                                    DicomTransferSyntax
                                                                                        .ExplicitVRLittleEndian,
                                                                                    DicomTransferSyntax
                                                                                        .ExplicitVRBigEndian,
                                                                                    DicomTransferSyntax
                                                                                        .ImplicitVRLittleEndian
                                                                            };

        private static DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
                                                                                 {
                                                                                         // Lossless
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14SV1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess14,
                                                                                         DicomTransferSyntax
                                                                                             .RLELossless,

                                                                                         // Lossy
                                                                                         DicomTransferSyntax
                                                                                             .JPEGLSNearLossless,
                                                                                         DicomTransferSyntax
                                                                                             .JPEG2000Lossy,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess1,
                                                                                         DicomTransferSyntax
                                                                                             .JPEGProcess2_4,

                                                                                         // Uncompressed
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRLittleEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ExplicitVRBigEndian,
                                                                                         DicomTransferSyntax
                                                                                             .ImplicitVRLittleEndian
                                                                                 };

        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, Dicom.Log.Logger log)
            : base(stream, fallbackEncoding, log)
        {

        }


        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            Console.WriteLine("received association request " + association.ToString());
            foreach (var pc in association.PresentationContexts)
            {
                //    if (pc.AbstractSyntax == DicomUID.Verification) pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                //    else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);

                pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);

            }

            SendAssociationAccept(association);

        }
        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {
            Console.WriteLine("received cstore request " + request.ToString());

            if (File.Exists("singleImage.txt"))
            {
                Console.WriteLine("now save on ./images/file.jpg");

                File.Delete("./images/file.dcm");
                var dicomFile = new DicomFile(request.File.Dataset);
                //  dicomFile.ChangeTransferSyntax(DicomTransferSyntax.JPEGProcess14SV1);
                dicomFile.Save("./images/file.dcm");
                var image = new DicomImage("./images/file.dcm");
                File.Delete("./images/file.jpg");
                image.RenderImage().AsClonedBitmap().Save("./images/file.jpg");
            }
            else // else save image to database
            {
                Console.WriteLine("now save in database" + Environment.NewLine);
                try
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
                    pathInDatabase = Path.Combine(pathInDatabase, date.Year.ToString(), date.Month.ToString(), date.Day.ToString(),
                        StudyInstanceUID, SeriesInstanceUID);
                    if (!Directory.Exists(pathInDatabase)) Directory.CreateDirectory(pathInDatabase);
                    string imagePath = Path.Combine(pathInDatabase,
                        SOPInstanceUID.Substring(SOPInstanceUID.Length - 3, 3)) + ".dcm";
                    if (!File.Exists(imagePath))
                    {
                        request.File.Save(imagePath);
                        Console.WriteLine("received and saved a file in database");
                    }
                    else Console.WriteLine("File already present in database");

                    //DicomAnonymizer.SecurityProfileOptions.
                    var profile = new DicomAnonymizer.SecurityProfile();
                    profile.PatientName = "random";
                    DicomAnonymizer anonymizer = new DicomAnonymizer(profile);
                    DicomDataset anonymizedDataset = anonymizer.Anonymize(request.Dataset);

                    // get more data
                    string PatientName = "";
                    anonymizedDataset.TryGetSingleValue(DicomTag.PatientName, out PatientName);
                    Console.WriteLine("patient name " + PatientName);
                    string PatientID = "";
                    anonymizedDataset.TryGetSingleValue(DicomTag.PatientID, out PatientID);


                    // add entry in database
                    var study = new StudyQueryOut
                    {
                        StudyInstanceUID = StudyInstanceUID,
                        PatientID = PatientID,
                        PatientName = PatientName,
                        StudyDate = date
                    };
                    using (var db = new LiteDatabase("./databaseFolder/database.db"))
                    {
                        var studies = db.GetCollection<StudyQueryOut>("studies");
                        if (studies.FindById(StudyInstanceUID) == null)
                            studies.Insert(study);
                    }
                }
                catch (Exception ec) { Console.WriteLine(ec.Message + "   " + ec.ToString()); }

            }
            return new DicomCStoreResponse(request, DicomStatus.Success);
        }
        public class StudyQueryOut
        {
            [BsonId]
            public string StudyInstanceUID { get; set; } = "";
            public string PatientID { get; set; } = "";
            public string PatientName { get; set; } = "";
            public DateTime StudyDate { get; set; } = new DateTime();
        }

        private void SendAssociationAccept(DicomAssociation association)
        {
            SendAssociationAcceptAsync(association);
        }

        public void OnReceiveAssociationReleaseRequest()
        {
            SendAssociationReleaseResponseAsync();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
        }

        public void OnConnectionClosed(Exception exception)
        {
        }

        public void OnCStoreRequestException(string tempFileName, Exception e)
        {
            // let library handle logging and error response
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            return Task.Run(() => { OnReceiveAssociationRequest(association); });
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return Task.Run(() => { OnReceiveAssociationReleaseRequest(); });
        }
    }
}
