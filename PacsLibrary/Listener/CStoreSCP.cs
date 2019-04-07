using Dicom;
using Dicom.Network;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PacsLibrary.Listener
{
    /// <summary>
    /// A type of <see cref="DicomService"/> used to listen for incoming DICOM files (implements C-StoreSCP of the DICOM standard).
    /// </summary>
    public class CStoreSCP : DicomService, IDicomServiceProvider, IDicomCStoreProvider, IDicomCEchoProvider
    {
        // accepted transfer syntaxes
        private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };

        // on association request
        public void OnReceiveAssociationRequest(DicomAssociation association)
        {
            Debug.receivedAssociationRequest(association);
            foreach (var pc in association.PresentationContexts) pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
            SendAssociationAccept(association);
        }

        // on cStore request
        public DicomCStoreResponse OnCStoreRequest(DicomCStoreRequest request)
        {
            return HandleIncomingFiles.onCStoreRequest(request);
        }

        //

        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, Dicom.Log.Logger log)
            : base(stream, fallbackEncoding, log) { }

        private void SendAssociationAccept(DicomAssociation association)
        {
            SendAssociationAcceptAsync(association);
        }

        public void OnReceiveAssociationReleaseRequest()
        {
            Debug.releaseAssociation();
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
