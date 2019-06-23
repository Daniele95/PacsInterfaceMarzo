Welcome to the PACS Interface written by Dr. Daniele Gamba for the Anatomage Table
built with the Fellow Oak Dicom v4.0.1 (fo-dicom.Desktop on NuGet) C# Library.

The core of the Program is the PacsLibrary, a C# class library dll. 
This Library implements the Dicom Commands (see the DICOM Standard)
- C-Echo: Tcp handshake
- C-FindSCU: Tcp query for image info. Implements STUDY, SERIES, and IMAGE query levels
- C-MoveSCU: sends the server a Move command, ie instructs the server to send DICOM data
- C-StoreSCP: listens for the incoming Tcp query, issued from the server,
				sending the required data. In this case the Pacs Interface will
				act as a SCP (Service Class Provider) instead of a SCU (Service Class User),
				by listening for incoming data.
				
Note: I chose not to implement the simpler command C-Get, which automatically performs both C-Move and C-Store by a single call to the server, without the need for the server to open a new connection on the client. That's because unfortunately C-Get is not widely supported by Dicom servers.
				
To test the library, a simple Guest User Interface is provided. 
You can find the executable to run in the GUI2/bin/Debug folder.
Before you run the GUI2.exe Interface, be sure you run Configuration.exe.

CONFIGURATION-------------------------------------------------------------------------
GUI2/bin/Debug/Configuration.exe
Here you can find a table of known servers (this table is stored in the servers.db database)

KNOWN SERVERS:
I hereby included the server I used to test the library, which is contained in the TestServer folder in the main directory. Click on it two times to set it up as the current server. Otherwise you can query 'dicomserver.co.uk', which contains a lot of good Anonymized Dicom Data. But be aware that in order to actually download data from this external server you need to forward the right ports (104 and 11112) of your router (and of course open them on your firewall) because the server will need to open a connection on them to send the data.

THIS NODE:
This is an info the server must know too for the C-Move command to work, for instance in my TestServer it is specified in the file 'ae.properties'.

USE TLS CRYPTOGRAPHY: be sure to enable it in case you want to query a server with enabled Tls Authentication (I included one in the TestServer folder). In this case be sure to specify the right path for the Key Store and Trust Store Locations (see end of this README)

The resulting configuration is written in the 'ServerConfig.txt' file

GUEST USER INTERFACE-------------------------------------------------------------------
GUI2/bin/Debug/GUI2.exe
Be sure to run this .exe as an administrator if you are using Tls authentication, otherwise it will be unable to access a certificate needed to download data from the server (this is actually a issue of the used library, Fellow Oak Dicom). (running this exe will automatically start also 'Listener.exe')

After you execute the query, you can double click on one of the Studies to show the Series contained in that Study. 'Click for preview' will download a sample of that series, ie an image taken from the half of the series, which we suppose is representative of the series. Double click on the series will download it and copy the full path for it on your machine.

To show the Studies you have already downloaded you can use the 'Local Studies' window (in the top bar of the window click 'Local Studies'), which have been anonymized if that option is turned on. Double click on one of them will show the contained Series, double click on a series will copy its full path on your machine.


NOTES ON THE IMPLEMENTATION-------------------------------------------------------------
About the PacsLibrary C# Project:
I encapsulated all the methods used to perform the queries in the 'QueryObject.cs', so this will be the one to call (as it is done for instance in line 14 of Program.cs of the 'GUI' C# Project)
This library creates a Synchronous, single-threaded implementation of the DICOM commands.
There will be two exes running at once (GUI + Listener), to communicate between them we use text files (for instance the 'pathForDownload.txt').

'QueryObject.cs' is actually a object-oriented wrapper class for the 'Query.cs', containing all the Query methods. To simplify the code, this and other classes makes heavy use of the 'QueryHelpers.cs', which are objects that store all the 'in' and 'out' text data we send to the server and receive from it.

The Query methods also contain several references to 'Configuration.cs', which is a serializable object contained all the info specified by the user in the 'Configuration.exe'.

Apart from those core classes, you will find 'LocalQuery.cs' which is used to bring about the Dicoms you have already downloaded and stored on your machine (and maybe anonymized).
The 'DesktopNetworkStream.cs' overrides a class in the Fo-Dicom Library and is called in 'Query.cs' to perform the query. The override was necessary to give adequates instruction for how to get the Tls Certificates, in case Tls authentication is enabled.
'Debug.cs' is just responsible for the text printed in the console (in the 'Listener.exe') and in the 'GUI2.exe' console, which is disabled by default but you can have it if you run 'GUI2.exe' from the terminal. Also, you can change the 'verbose' boolean in 'Debug.cs' to get more info.


You then have the Listener classes, which include 'InitListener.cs', which starts a new DicomServer of type 'CStoreSCP.cs' (using also Tls if specified)
'CStoreSCP.cs' contains all the methods the listener uses to handle incoming connections
The 'HandleIncomingFiles.cs' actually isolates the most importants of these methods and implements them as required (ie saves the data sent from the server).


GENERATE A KEY - TRUST CERTIFICATES FOR TLS AUTHENTICATION--------------------------------------------
Here is how to generate the needed server-client certificates for tls authentication.
The TestServer we use (dcm4che) is Java so it easily reads .jks format (java certificates) 
so we will generate the needed ones with the 'Keytool' java utility.

insert instead of 'rama' your machine's name, and instead of 'daniele' your name:

# keytool -genkey -keyalg RSA -dname "CN=rama.dcm4che.org OU=daniele O=rama L=Milano C=IT" --keystore rama.jks -alias rama

# keytool -export -file rama.cert -keystore rama.jks -alias rama

# keytool -import -file rama.cert -keystore trust.jks -alias rama

now we have the certificates for the dcm4che server. To put them inside our program (in the configuration) we need them in .p12 format:

# keytool -importkeystore -srckeystore mySrvKeystore -destkeystore mySrvKeystore.p12 -srcstoretype JKS -deststoretype PKCS12