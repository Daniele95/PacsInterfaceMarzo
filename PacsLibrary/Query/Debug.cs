using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacsLibrary.Configurator;

namespace PacsLibrary.Query
{

    internal class Debug
    {
        static void line()
        {
            Console.WriteLine("-------------------------------------------------"
                + Environment.NewLine);
        }

        static void breakLine()
        {
            Console.Write(Environment.NewLine + Environment.NewLine);
        }

        public static void welcome()
        {
            Console.Write("Welcome to the Pacs Interface of the Anatomage Table!");
            breakLine();
        }
        public static void gotNumberOfResults(int count)
        {
            Console.Write("got " + count.ToString()); breakLine();
            breakLine();
        }
        public static void studyQuery(Configuration configuration, Study studyQuery)
        {
            Console.Write("Querying server " + configuration.host + ":" + configuration.port
                + " for STUDIES");
            breakLine();
        }
        public static void seriesQuery(Configuration configuration, Series seriesTemplate)
        {
            line();
            Console.Write("Querying server " + configuration.host + ":" + configuration.port
                + " for SERIES in study no. " + seriesTemplate.getStudyInstanceUID());
            breakLine();
        }
        public static void cantReachServer()
        {
            Console.Write("Impossible to reach the server");
            breakLine();
        }

        public static void downloading(Configuration configuration)
        {
            Console.Write("Downloading series from server " + configuration.host + ":" + configuration.port);
            breakLine();
        }
        public static void done()
        {
            Console.Write("Done."); breakLine();
        }
        public static void downloadingImage(Configuration configuration, string SOPInstanceUID)
        {
            Console.Write("Downloading from server "
                + configuration.host + ":" + configuration.port
                + " sample image no. " + SOPInstanceUID);
            breakLine();
        }
        public static void imageQuery(Configuration configuration, Series seriesResponse)
        {
            Console.Write("Querying server " + configuration.host + ":" + configuration.port +
                       " for IMAGES in series no. " + seriesResponse.getSeriesInstanceUID());
            breakLine();
        }
    }
}
