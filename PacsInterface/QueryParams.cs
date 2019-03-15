using Dicom;
using Dicom.Network;
using GUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;

namespace PacsInterface
{
    public class QueryParams
    {
        public class SeriesQueryOut
        {
            public string SeriesDescription { get; set; } = "";
            public string StudyDate { get; set; } = "";
            public string Modality { get; set; } = "";
            public string SeriesInstanceUID { get; set; } = "";
            public string StudyInstanceUID { get; set; } = "";

            public SeriesQueryOut(DicomCFindResponse response)
            {
                PropertyInfo[] properties = typeof(SeriesQueryOut).GetProperties();
                foreach (var property in properties)
                {
                    // (0010,0010)
                    string tag = typeof(DicomTag).GetField(property.Name).GetValue(null).ToString();
                    // DicomTag.PatientName
                    DicomTag theTag = (DicomTag.Parse(tag));

                    string value = "";
                    response.Dataset.TryGetSingleValue(theTag, out value);
                    property.SetValue(this, value);
                }
            }
        }

        public class StudyQueryOut
        {
            public string StudyInstanceUID { get; set; } = "";
            public string PatientID { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string StudyDate { get; set; } = "";
            public string ModalitiesInStudy { get; set; } = "";
            public string StudyDescription { get; set; } = "";

            public StudyQueryOut() { }
            public StudyQueryOut(DicomCFindResponse response)
            {
                PropertyInfo[] properties = typeof(StudyQueryOut).GetProperties();
                foreach(var property in properties)
                {
                    // (0010,0010)
                    string tag = typeof(DicomTag).GetField(property.Name).GetValue(null).ToString();
                    // DicomTag.PatientName
                    DicomTag theTag = (DicomTag.Parse(tag));

                    if (property.Name != "ModalitiesInStudy")
                    {
                        string value = "";
                        response.Dataset.TryGetSingleValue(theTag, out value);
                        property.SetValue(this, value);
                    }
                    else
                    {
                        var value = new string[5];
                        response.Dataset.TryGetValues(theTag, out value);
                        string modalities = "";
                        if(value != null)
                            for (int i=0;i<value.Length;i++) modalities = modalities + value[i]+", ";
                        if (modalities.Length > 2) modalities = modalities.Substring(0,modalities.Length - 2);
                        property.SetValue(this, modalities);
                    } 
                }
            }
        }

        public class StudyQueryIn
        {
            public string StudyInstanceUID { get; set; } = "";
            public string PatientID { get; set; } = "";
            public string PatientName { get; set; } = "";
            public DicomDateRange StudyDate { get; set; } = new DicomDateRange();
            public string ModalitiesInStudy { get; set; } = "";

            public StudyQueryIn(QueryPage queryPage)
            {
                var dateMin = queryPage.StudyDateStartPicker.SelectedDate;
                var dateMax = queryPage.StudyDateEndPicker.SelectedDate;
                // read search fields
                DateTime start = DateTime.Today.AddYears(-100), end = DateTime.Today;
                if (dateMin != null)
                {
                    start = queryPage.StudyDateStartPicker.SelectedDate.Value;
                    end = start.AddDays(1);
                }
                if (dateMax != null)
                {
                    end = queryPage.StudyDateEndPicker.SelectedDate.Value;
                    start = end.AddDays(-1);
                }
                end = end.AddSeconds(86399);

                StudyDate = new DicomDateRange(start, end);
                PatientName = patientFullName(queryPage.PatientNameBox, queryPage.PatientSurnameBox);
                ModalitiesInStudy = queryPage.ModalityBox.Text.ToString();
                PatientID = queryPage.PatientIDBox.Text.ToString();
            }

            public override string ToString()
            {
                string ret = "";
                if (PatientID != "") ret = ret + "PatientID: " + PatientID+Environment.NewLine;
                if (PatientName != "") ret = ret + "PatientName: " + PatientName + Environment.NewLine;
                if (StudyDate != null) ret = ret + "StudyDate: " + StudyDate + Environment.NewLine;
                if (ModalitiesInStudy != "") ret = ret + "ModalitiesInStudy: " + ModalitiesInStudy + Environment.NewLine;
                return ret;
            }
        }

        // util
        static string patientFullName(TextBox patientNameBox, TextBox patientSurnameBox)
        {
            string name = patientNameBox.Text.ToString();
            string surname = patientSurnameBox.Text.ToString();

            string patientFullName = "";
            if (name != "" && surname != "") patientFullName += surname + "^" + name;
            if (name == "") patientFullName += "*" + surname + "*";
            if (surname == "") patientFullName += "*" + name + "*";

            return patientFullName;
        }
    }
}
