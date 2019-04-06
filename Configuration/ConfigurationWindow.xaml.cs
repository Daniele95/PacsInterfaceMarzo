using Dicom.Network;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using PacsLibrary;

namespace Configuration
{
    public partial class ConfigurationWindow : Window
    {
        public class server
        {
            [BsonId]
            public string ip { get; set; }
            public string port { get; set; }
            public string AET { get; set; }
        }

        List<CheckBox> StudyProperties;
        List<CheckBox> SeriesProperties;

        PacsLibrary.Configuration configuration;

        public ConfigurationWindow()
        {
            InitializeComponent();

            configuration = new PacsLibrary.Configuration("ServerConfig.txt");

            // show current configuration
            currentServer.Text = configuration.AET + "@" +
                configuration.host + ":" + configuration.port;
            thisNodesName.Text = configuration.thisNodeAET + " " +
                configuration.thisNodePort;
            anonymizeDataCheckbox.IsChecked = configuration.anonymizeData;
            destinationBox.Text = configuration.fileDestination;

            useTlsCheckBox.IsChecked = configuration.useTls;
            trustStorePathField.Text = configuration.trustStorePath;
            trustStorePasswordField.Text = configuration.trustStorePassword;
            //

            // show list of known servers
            PropertyInfo[] properties = typeof(server).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = property.Name,
                    DisplayMemberBinding = new Binding(property.Name),
                });
            }

            using (var db = new LiteDatabase("servers.db"))
            {
                var servers = db.GetCollection<server>("servers");
                var allServers = servers.FindAll();
                foreach (var server in allServers)
                {
                    listView.Items.Add(server);
                }
            }
            //

            // configure columns to show as query result
            StudyProperties = new List<CheckBox>();
            foreach (var property in configuration.studyTemplate)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Content = property.name;
                checkBox.IsChecked = property.visible;
                checkBox.Checked += (a, b) => { property.visible = true; configuration.write(); };
                checkBox.Unchecked += (a, b) => { property.visible = false; configuration.write(); };
                studyPanel.Children.Add(checkBox);
                StudyProperties.Add(checkBox);
            }

            SeriesProperties = new List<CheckBox>();
            foreach (var property in configuration.seriesTemplate)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Content = property.name;
                checkBox.IsChecked = property.visible;
                checkBox.Checked += (a, b) => { property.visible = true; configuration.write(); };
                checkBox.Unchecked += (a, b) => { property.visible = false; configuration.write(); };
                seriesPanel.Children.Add(checkBox);
                SeriesProperties.Add(checkBox);
            }
            //
        }

        private void setThisNodesName(object sender, RoutedEventArgs e)
        {
            SetThisNodeNamePopup popup = new SetThisNodeNamePopup();
            popup.setButton.Click += (a, b) =>
            {
                string thisNodeAET = popup.AETTextBox.Text;
                string thisNodePort = popup.portTextBox.Text;
                int thisNodePortInt = 0;
                bool portIsInt = int.TryParse(thisNodePort, out thisNodePortInt);
                if (thisNodeAET == "" || thisNodePort == "") MessageBox.Show("Fields cannot be empty");
                else if (!portIsInt) MessageBox.Show("Port must be an integer number");
                else
                {
                    configuration.thisNodeAET = thisNodeAET;
                    configuration.thisNodePort = int.Parse(thisNodePort);
                    configuration.write();
                    thisNodesName.Text = thisNodeAET + " " + thisNodePort;
                }
                popup.Close();
            };
            popup.AETTextBox.Text = configuration.thisNodeAET;
            popup.portTextBox.Text = configuration.thisNodePort.ToString();
            popup.ShowDialog();
        }


        private void editSelectedServer(object sender, RoutedEventArgs e)
        {
            EditServerPopup popup = new EditServerPopup();

            if (listView.Items.Count != 0)
            {
                server selectedServer = listView.SelectedItem as server;
                if (selectedServer == null) selectedServer = listView.Items[0] as server;

                popup.informative_textblock2.Text = selectedServer.ip;
                popup.portTextBox.Text = selectedServer.port;
                popup.AETTextBox.Text = selectedServer.AET;

                popup.addButton.Click += (a, b) =>
                {
                    using (var db = new LiteDatabase("servers.db"))
                    {
                        var servers = db.GetCollection<server>("servers");
                        server selectedServerInDb = servers.FindById(selectedServer.ip);

                        // update database
                        selectedServerInDb.port = popup.portTextBox.Text;
                        selectedServerInDb.AET = popup.AETTextBox.Text;
                        servers.Update(selectedServerInDb);

                        // also update listview 
                        listView.Items.Clear();
                        var allServers = servers.FindAll();
                        foreach (var item in allServers)
                        {
                            listView.Items.Add(item);
                        }
                    }

                    popup.Close();
                };
                popup.Show();

            }
        }

        private void testSelectedServer(object sender, RoutedEventArgs e)
        {
            try
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


                if (listView.Items.Count != 0)
                {
                    server selectedServer = listView.SelectedItem as server;
                    if (selectedServer == null) selectedServer = listView.Items[0] as server;
                    string info = "Tested connection to: " + selectedServer.AET + "@" + selectedServer.ip + ":" + selectedServer.port
                            + Environment.NewLine;
                    try
                    {
                        client.Send(selectedServer.ip, int.Parse(selectedServer.port), false, configuration.thisNodeAET, selectedServer.AET);
                        if (result) MessageBox.Show(info + "Success");
                        else MessageBox.Show(info + "Server did not respond correctly");
                    }
                    catch (Exception ec)
                    {
                        MessageBox.Show(info + "Could not reach server");
                    }
                }

            }
            catch (Exception ee) { MessageBox.Show(ee.Message); }


        }

        private void addNewServer(object sender, RoutedEventArgs e)
        {
            AddServerPopup popup = new AddServerPopup();
            popup.addButton.Click += (a, b) =>
            {
                string port = popup.portTextBox.Text;
                int portInt = 0;
                bool portIsInt = (port != "") && (int.TryParse(port, out portInt));

                if (popup.hostTextBox.Text != "" && portIsInt)
                    using (var db = new LiteDatabase("servers.db"))
                    {
                        var servers = db.GetCollection<server>("servers");
                        var newServer = new server
                        {
                            ip = popup.hostTextBox.Text,
                            port = popup.portTextBox.Text,
                            AET = popup.AETTextBox.Text
                        };
                        server alreadyInDB = servers.FindById(newServer.ip);
                        if (alreadyInDB == null)
                        {
                            servers.Insert(newServer);
                            listView.Items.Add(newServer);
                        }
                        else
                        {
                            // update database
                            alreadyInDB.port = newServer.port;
                            alreadyInDB.AET = newServer.AET;
                            servers.Update(alreadyInDB);
                            // also update listview 
                            listView.Items.Clear();
                            var allServers = servers.FindAll();
                            foreach (var item in allServers)
                            {
                                listView.Items.Add(item);
                            }
                        }
                    }
                popup.Close();
            };
            popup.ShowDialog();
        }

        private void removeSelectedServer(object sender, RoutedEventArgs e)
        {
            server selectedServer = listView.SelectedItem as server;
            if (selectedServer != null)
            {
                listView.Items.Remove(selectedServer);
                using (var db = new LiteDatabase("servers.db"))
                {
                    var servers = db.GetCollection<server>("servers");
                    servers.Delete(selectedServer.ip);
                }
            }
        }


        private void setCurrentServer(object sender, RoutedEventArgs e)
        {
            if (listView.Items.Count != 0)
            {
                server listViewItem = listView.SelectedItem as server;
                if (listViewItem == null) listViewItem = listView.Items[0] as server;
                currentServer.Text = listViewItem.AET + "@" + listViewItem.ip + ":" + listViewItem.port;

                configuration.host = listViewItem.ip;
                configuration.port = int.Parse(listViewItem.port);
                configuration.AET = listViewItem.AET;
                configuration.write();
            }
        }

        private void anonymizeDataCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            configuration.anonymizeData = bool.Parse(anonymizeDataCheckbox.IsChecked.ToString());
            configuration.write();
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                destinationBox.Text = openFolderDialog.SelectedPath;

            if (configuration != null)
            {
                configuration.fileDestination = destinationBox.Text;
                configuration.write();
            }
        }

        private void keyStoreLocationBrowse_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFolderDialog = new System.Windows.Forms.OpenFileDialog();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                KeyStorePathField.Text = openFolderDialog.FileName;
        }

        private void trustStoreLocationBrowse_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFolderDialog = new System.Windows.Forms.OpenFileDialog();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                trustStorePathField.Text = openFolderDialog.FileName;
        }

        private void useTlsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            configuration.useTls = bool.Parse(useTlsCheckBox.IsChecked.ToString());
            configuration.write();
        }

        private void setKeyStore(object sender, RoutedEventArgs e)
        {
            try
            {
                if (configuration != null)
                {
                    X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadWrite);
                    var oldCerts = store.Certificates.Find(X509FindType.FindBySubjectName, configuration.keyStoreName, false);
                    foreach (var cert2 in oldCerts) MessageBox.Show(configuration.keyStoreName + " " +cert2.Subject);
                    if(oldCerts != null && oldCerts.Count>0) store.Remove(oldCerts[0]);
                    foreach (var myCert in store.Certificates) { store.Remove(myCert); MessageBox.Show("rimuovo certificato"); }
                    try
                    {
                        X509Certificate2 newCert = new X509Certificate2(KeyStorePathField.Text, keyStorePasswordField.Text);
                        store.Add(newCert);
                        // check this line
                        configuration.keyStoreName = newCert.GetNameInfo(X509NameType.SimpleName, false).Split(' ')[0];
                        configuration.write();
                    }
                    catch (Exception) { MessageBox.Show("Incorrect certificate path or password"); }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void setTrustStore(object sender, RoutedEventArgs e)
        {
            if (configuration != null)
            {
                configuration.trustStorePath = trustStorePathField.Text;
                configuration.trustStorePassword = trustStorePasswordField.Text;
                configuration.write();
            }
        }
    }
}
