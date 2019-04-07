using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacsLibrary.Configurator
{
    public static class CECHO
    {
        public static bool Send(string host, int port, string AET, string thisNodeAET)
        {
            var server = new DicomServer<DicomCEchoProvider>();

            var client = new DicomClient();
            client.NegotiateAsyncOps();
            bool result = true;

            for (int i = 0; i < 10; i++)
            {
                var request = new DicomCEchoRequest();
                request.OnResponseReceived = (req, response) =>
                {
                    if (response.Status.ToString() != "Success") result = false;
                };
                client.AddRequest(request);
            }

            try
            {
                client.Send(host, port, false, thisNodeAET, AET);
            }
            catch (Exception) { }
            return result;
        }

    }
}
