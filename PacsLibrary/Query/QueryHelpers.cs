using Dicom;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacsLibrary.Query
{
    /// <summary>
    /// A parameter used to specify a query, identified by name (its DICOM tag, eg "PatientName"),
    /// value (eg "Doe^Pierre"), and visibility in the query results.
    /// </summary>
    [Serializable]
    public class QueryParameter
    {
        public string name { get; set; } = "";
        public string value { get; set; } = "";
        public bool visible { get; set; } = true;

        /// <summary>
        /// Given the QueryParameter name (eg. "StudyInstanceUID"), returns the corresponding element
        /// of the DicomTag enumerator (eg. DicomTag.StudyInstanceUID).
        /// </summary>
        /// <returns>An element of the DicomTag enumerator.</returns>
        public DicomTag getTag()
        {
            string tagNumber = typeof(DicomTag).GetField(name).GetValue(null).ToString();
            return DicomTag.Parse(tagNumber);
        }
    }

    /// <summary>
    /// Contains query parameters for a STUDY level query.
    /// A Study of a certain patient is an ensemble of medical images (Series).
    /// </summary>
    [Serializable]
    public class Study : List<QueryParameter>
    {
        public Study() { }

        /// <summary>
        /// Fills this Study with the <see cref="DicomDataset"/> obtained from a STUDY level query.
        /// </summary>
        /// <param name="dataset">Dataset containing the result of a STUDY level query.</param>
        /// <param name="studyQuery">Empty Study indicating which <see cref="QueryParameter"/> to extract 
        /// from the <see cref="DicomDataset"/>.</param>
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

        /// <summary>
        /// Returns the StudyInstanceUID of this Study - DICOM tag (0020,000D).
        /// </summary>
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

        /// <summary>
        /// Gets the list of QueryParameters as a dynamic Expando Object,
        /// useful to insert in a table.
        /// </summary>
        /// <returns>An Expando Object corresponding to this Study.</returns>
        public dynamic getDynamic()
        {
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var studyParameter in this)
                expando[studyParameter.name] = studyParameter.value;
            return expando as dynamic;
        }

        public void ToString()
        {
            foreach (var par in this)
                Console.WriteLine(par.name + " " + par.value + Environment.NewLine);
        }
    }

    /// <summary>
    /// Contains query parameters for a SERIES level query.
    /// A Series is a 2d or 3d medical image stored in the PACS, that can be the result of a
    /// Magnetic Resonance, PET, TAC, et cetera.
    /// It can contain from one to a hundred or more medical (.dcm) images, which are
    /// the "slices" composing the full 3d image.
    /// In other cases it can contain all other kinds of medical documents,
    /// also stored as .dcm images.
    /// </summary>
    [Serializable]
    public class Series : Study
    {
        public Series() { }

        /// <summary>
        /// Fills this Series with the <see cref="DicomDataset"/> resulting from a SERIES level query.
        /// </summary>
        /// <param name="dataset">Dataset containing the result of the SERIES level query.</param>
        /// <param name="seriesQuery">List of empty QueryParameter indicating which <see cref="QueryParameter"/> to extract 
        /// from the <see cref="DicomDataset"/>.</param>
        public Series(DicomDataset dataset, Series seriesQuery) : base(dataset, seriesQuery) { }

        protected string SeriesInstanceUID = "";

        protected string StudyDate = "";

        /// <summary>
        /// Returns the SeriesInstanceUID of this Series - DICOM tag (0020,000E).
        /// </summary>
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

        /// <summary>
        /// Initializes the StudyInstanceUID of this Series, obtained from the father Study
        /// in the hierarchical DICOM query model.
        /// </summary>
        /// <param name="incomingStudy">The Study containing this Series in the hierarchical DICOM query model.</param>
        public void setStudyInstanceUID(Study incomingStudy)
        {
            StudyInstanceUID = incomingStudy.getStudyInstanceUID();

            foreach (var queryParameter in this)
                if (queryParameter.name == "StudyInstanceUID")
                    queryParameter.value = StudyInstanceUID;
        }

        /// <summary>
        /// Gets the destination where this Series will be stored, when downloaded.
        /// The path depends on the StudyDate (if anonymized, the date of the download), plus 
        /// its StudyInstanceUID and SeriesInstanceUID. Each .dcm image is then saved in this path with its 
        /// SOPinstanceUID.
        /// </summary>
        /// <param name="fileDestination">The destination of the DICOM files, specified by the user in the <see cref="Configuration"/>.</param>
        /// <returns>The calculated path for saving this Series.</returns>
        public string getFullPath(string fileDestination)
        {
            return Path.Combine(
                fileDestination,
                DateTime.Now.Year.ToString(),
                DateTime.Now.Month.ToString(),
                DateTime.Now.Day.ToString(),
                this.getStudyInstanceUID(),
                this.getSeriesInstanceUID());
        }

        /// <summary>
        /// Returns the StudyDate of this Series - DICOM tag (0008,0020).
        /// </summary>
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

}
