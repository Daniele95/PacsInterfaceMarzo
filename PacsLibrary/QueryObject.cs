using PacsLibrary.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using PacsLibrary.Configurator;

namespace PacsLibrary
{
    /// <summary>
    /// Contains all the methods for querying and downloading images from
    /// a PACS, plus Server and Client configuration info.
    /// </summary>
    public class QueryObject
    {
        Configuration configuration;

        /// <summary>
        /// Initializes Server and Client configuration
        /// info which have been specified in the <see cref="Configuration"/> object
        /// which is serialized as a .txt file.
        /// </summary>
        public QueryObject()
        {
            configuration = new Configuration();
        }

        /// <summary>
        /// Returns an empty <see cref="Study"/> indicating which <see cref="QueryParameter"/>
        /// to show in the query results, depending on user configuration.
        /// </summary>
        /// <returns>A <see cref="Study"/>, which is a <see cref="List"/> of <see cref="QueryParameter"/>.</returns>
        public Study getStudyTemplate() { return configuration.studyTemplate; }

        /// <summary>
        /// Returns an empty <see cref="Series"/> indicating which <see cref="QueryParameter"/>
        /// to show in the query results, depending on user configuration.
        /// </summary>
        /// <returns>A <see cref="Series"/>, which is a <see cref="List"/> of <see cref="QueryParameter"/>.</returns>
        public Series getSeriesTemplate() { return configuration.seriesTemplate; }

        /// <summary>
        /// Queries the server indicated in <see cref="Configuration"/>
        /// with the query parameters indicated in <see cref="Study"/>. 
        /// Implements the DICOM C-FIND command at STUDY Level.
        /// </summary>
        /// <param name="configuration">Server and client configuration.</param>
        /// <param name="studyQuery">Parameters specifying the query.</param>
        public List<Study> CFINDStudies(Study studyQuery)
        {
            return PacsLibrary.Query.Query.CFINDStudies(configuration, studyQuery);
        }

        /// <summary>
        /// Queries the server indicated in <see cref="Configuration"/>
        /// for the various Series contained 
        /// in the specified <see cref="Study"/>. 
        /// Implements the DICOM C-FIND command at SERIES Level.
        /// </summary>
        /// <param name="configuration">Server and client configuration.</param>
        /// <param name="studyQuery">Parameters specifying the query.</param>
        public List<Series> CFINDSeries(Study studyResponse)
        {
            return PacsLibrary.Query.Query.CFINDSeries(configuration, studyResponse);
        }

        /// <summary>
        /// Asks the server indicated in <see cref="Configuration"/> 
        /// to send the specified <see cref="Series"/>.
        /// Implements the DICOM C-MOVE Command at SERIES level.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <returns>The path where the series has been saved.</returns>
        public string CMOVESeries(Series seriesResponse)
        {
            return PacsLibrary.Query.Query.CMOVESeries(configuration, seriesResponse);
        }

        /// <summary>
        /// Downloads an image representative of a given <see cref="Series"/>. 
        /// In this case, the image in the middle of the Series.
        /// </summary>
        /// <param name="configuration">Server and Client configuration.</param>
        /// <param name="seriesResponse">Parameters specifying the query.</param>
        /// <returns>The downloaded image as a BitmapImage.</returns>
        public BitmapImage downloadSampleImage(Series seriesResponse)
        {
            return PacsLibrary.Query.Query.downloadSampleImage(configuration, seriesResponse);
        }




        /// <summary>
        /// Gets the path of file storage specified by the user in the <see cref="Configuration"/>.
        /// </summary>
        /// <returns></returns>
        public string getLocalFilesDestination() { return configuration.fileDestination; }

        /// <summary>
        /// Searches for Studies in the local path, specified by the user in the <see cref="Configuration"/>,
        /// and indexed by the DICOMDIR database.
        /// Implements a local C-FIND at STUDY level command of the DICOM standard
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        public List<Study> CFINDLocalStudies()
        {
            return PacsLibrary.LocalQuery.LocalQuery.CFINDLocalStudies(configuration);
        }

        /// <summary>
        /// Searches for the Series matching a given <see cref="Study"/>, in the local path specified by the user in the <see cref="Configuration"/>, and indexed by the DICOMDIR database.
        /// Implements a local C-FIND at SERIES level command of the DICOM standard.
        /// Implements 
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        public List<Series> CFINDLocalSeries(Study study)
        {
            return PacsLibrary.LocalQuery.LocalQuery.CFINDLocalSeries(configuration, study);

        }

        /// <summary>
        /// Gets an image representative of the <see cref="Series"/> in the local database.
        /// </summary>
        /// <param name="configuration">Info on the path and hierarchy used to store the DICOM files.</param>
        /// <param name="series"></param>
        /// <returns></returns>
        public BitmapImage getThumb(Series series)
        {
            return PacsLibrary.LocalQuery.LocalQuery.getThumb(configuration, series);
        }

    }
}
