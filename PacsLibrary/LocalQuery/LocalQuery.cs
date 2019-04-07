using Dicom;
using Dicom.Imaging;
using Dicom.Media;
using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PacsLibrary.LocalQuery
{
    public class LocalQuery
    {
        /// <summary>
        /// Searches for Studies in the local path, specified by the user in the <see cref="Configuration"/>,
        /// and indexed by the DICOMDIR database.
        /// Implements a local C-FIND at STUDY level command of the DICOM standard
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        public static List<Study> CFINDLocalStudies(Configuration configuration)
        {
            string dicomDirPath = Path.Combine(configuration.fileDestination, "DICOMDIR");

            // prepare to receive data
            var localStudyResponses = new List<Study>();

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir != null)
                    foreach (var patientRecord in dicomDir.RootDirectoryRecordCollection)
                    {
                        foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                        {
                            studyRecord.Add(DicomTag.PatientName, patientRecord.GetSingleValue<string>(DicomTag.PatientName));
                            studyRecord.Add(DicomTag.PatientID, patientRecord.GetSingleValue<string>(DicomTag.PatientID));
                            Study myStudy = new Study(studyRecord, configuration.studyTemplate);
                            localStudyResponses.Add(myStudy);
                        }
                    }
            }
            return localStudyResponses;

        }

        /// <summary>
        /// Searches for the Series matching a given <see cref="Study"/>, in the local path specified by the user in the <see cref="Configuration"/>, and indexed by the DICOMDIR database.
        /// Implements a local C-FIND at SERIES level command of the DICOM standard.
        /// Implements 
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        public static List<Series> CFINDLocalSeries(Configuration configuration, Study study )
        {
            string dicomDirPath = Path.Combine(configuration.fileDestination, "DICOMDIR");

            // prepare to receive data
            var localSeriesResponses = new List<Series>();

            using (var fileStream = new FileStream(dicomDirPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var dicomDir = DicomDirectory.Open(fileStream);
                if (dicomDir != null)
                    foreach (var patientRecord in dicomDir.RootDirectoryRecordCollection)
                    {
                        foreach (var studyRecord in patientRecord.LowerLevelDirectoryRecordCollection)
                        {
                            if (studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID) ==
                                study.getStudyInstanceUID())
                                foreach (var seriesRecord in studyRecord.LowerLevelDirectoryRecordCollection)
                                {
                                    seriesRecord.Add(DicomTag.StudyInstanceUID,
                                        studyRecord.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                                    seriesRecord.Add(DicomTag.StudyDate,
                                        studyRecord.GetSingleValue<string>(DicomTag.StudyDate));
                                    Series mySeries = new Series(seriesRecord, configuration.seriesTemplate);
                                    localSeriesResponses.Add(mySeries);
                                }
                        }
                    }
            }
            return localSeriesResponses;

        }

        /// <summary>
        /// Gets an image representative of the <see cref="Series"/> in the local database.
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        /// <param name="series"></param>
        /// <returns></returns>
        public static BitmapImage getThumb(Configuration configuration,Series series)
        {
            var imageJpg = new BitmapImage();
            string seriesPath = series.getFullPath(configuration.fileDestination);
            string thumbPath = Path.Combine(seriesPath, "thumb.jpg");
            try
            {
                if (!File.Exists(thumbPath))
                {
                    var files = Directory.GetFiles(seriesPath).OrderBy(name => name).ToArray();
                    string imagePath = files[(int)(files.Length / 2.0f)];
                    var thumb = new DicomImage(imagePath);
                    thumb.RenderImage().AsClonedBitmap().Save(Path.Combine(thumbPath));
                }
                var uriSource = new Uri(Path.GetFullPath(thumbPath));
                imageJpg.BeginInit();
                imageJpg.CacheOption = BitmapCacheOption.OnLoad;
                imageJpg.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imageJpg.UriSource = uriSource;
                imageJpg.EndInit();

            }
            catch (Exception e) { }
            return imageJpg;
        }
    }
}
