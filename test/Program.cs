using Dicom;
using Dicom.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\Users\daniele\Desktop\dicoms";

            var studyDir = new DirectoryInfo(Path.Combine(path,"myfolder"));
            var dicomDirPath = Path.Combine(path, "DICOMDIR");
            var dicomDir = new DicomDirectory();

            foreach (var file in studyDir.GetFiles("*.dcm*", SearchOption.AllDirectories))
            {
                var dicomFile = DicomFile.Open(file.FullName);
                Console.WriteLine(dicomFile.Dataset.GetValue<string>(DicomTag.StudyInstanceUID,0));
                dicomDir.AddFile(dicomFile, file.Name);
            }

            dicomDir.Save(dicomDirPath);

            var dicomDir2= DicomDirectory.Open(new FileStream(@"C:\Users\daniele\Desktop\dicoms\DICOMDIR",FileMode.Open));

            Console.WriteLine(dicomDir2.Dataset.First().Tag);
            foreach (var patientRecord in dicomDir2.RootDirectoryRecordCollection)
            {
                Console.WriteLine(
                    "Patient: {0} ({1})",
                    patientRecord.GetSingleValue<string>(DicomTag.PatientName),
                    patientRecord.GetSingleValue<string>(DicomTag.PatientID));
                

                foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                {
                    Console.WriteLine("\tStudy: {0}", studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID));

                    foreach (var seriesRecord in studyRecord.LowerLevelDirectoryRecordCollection)
                    {
                        Console.WriteLine("\t\tSeries: {0}", seriesRecord.GetSingleValue<string>(DicomTag.SeriesInstanceUID));

                        foreach (var imageRecord in seriesRecord.LowerLevelDirectoryRecordCollection)
                        {
                            Console.WriteLine(
                                "\t\t\tImage: {0} [{1}]",
                                imageRecord.GetSingleValue<string>(DicomTag.ReferencedSOPInstanceUIDInFile),
                                imageRecord.GetSingleValue<string>(DicomTag.ReferencedFileID));
                        }
                    }
                }
            }

            while (true) { }
        }
    }
}
