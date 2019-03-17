using Dicom.Network;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Configuration
{
    public class CurrentConfiguration
    {
        public string ip { get; set; }
        public string port { get; set; }
        public string AET { get; set; }
        public bool anonymizeData { get; set; }
        public string thisNodeAET { get; set; }
        public string thisNodePort { get; set; }
        public string fileDestination { get; set; }

        public CurrentConfiguration()
        {
            var lines = File.ReadAllLines("ServerConfig.txt");
            ip = lines[0];
            port = lines[1];
            AET = lines[2];
            anonymizeData = bool.Parse(lines[3]);
            thisNodeAET = lines[4];
            thisNodePort = lines[5];
            fileDestination = lines[6];
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try { stream = file.Open(System.IO.FileMode.Open, FileAccess.Read, FileShare.None); }
            catch (IOException) { return true; }
            finally { if (stream != null) stream.Close(); }
            return false;
        }

        public void writeDown()
        {
            bool locked = true;
            while (locked) locked = IsFileLocked(new FileInfo("ServerConfig.txt"));
            if (!locked)
            {
                var lines = File.ReadAllLines("ServerConfig.txt");
                lines[0] = ip;
                lines[1] = port;
                lines[2] = AET;
                lines[3] = anonymizeData.ToString();
                lines[4] = thisNodeAET;
                lines[5] = thisNodePort;
                lines[6] = fileDestination;
                File.WriteAllLines("ServerConfig.txt", lines);
            }
        }

    }

    public partial class ConfigurationWindow : Window
    {
        public class server
        {
            [BsonId]
            public string ip { get; set; }
            public string port { get; set; }
            public string AET { get; set; }
        }

        // query show options
        List<CheckBox> StudyProperties;
        List<CheckBox> SeriesProperties;
        public class Study
        {
            public string StudyInstanceUID { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string PatientID { get; set; } = "";
            public string StudyDate { get; set; } = "";
            public string ModalitiesInStudy { get; set; } = "";
            public string PatientBirthDate { get; set; } = "";
            public string StudyDescription { get; set; } = "";
            public string AccessionNumber { get; set; } = "";
        }
        public class Series
        {
            public string SeriesDescription { get; set; } = "";
            public string StudyDate { get; set; } = "";
            public string Modality { get; set; } = "";
            public string SeriesInstanceUID { get; set; } = "";
            public string StudyInstanceUID { get; set; } = "";
        }
        //

        bool isContained(string file, string content)
        {
            bool isContained = true;
            string[] ls = File.ReadAllLines(file);
            List<string> list = ls.ToList();
            if (!list.Contains(content))
                isContained = false;
            return isContained;
        }
        void onChecked(string file, string content)
        {
            if (!isContained(file, content))
                File.AppendAllText(file, content + Environment.NewLine);
        }
        void unCkecked(string file, string content)
        {
            File.WriteAllLines(file, File.ReadLines(file).Where(l => l != content).ToList());
        }

        CurrentConfiguration configuration;

        public ConfigurationWindow()
        {
            InitializeComponent();

            configuration = new CurrentConfiguration();

            // show current configuration
            currentServer.Text = configuration.AET + "@" +
                configuration.ip + ":" + configuration.port;
            thisNodesName.Text = configuration.thisNodeAET + " " +
                configuration.thisNodePort;
            anonymizeDataCheckbox.IsChecked = configuration.anonymizeData;
            destinationBox.Text = configuration.fileDestination;
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
            PropertyInfo[] studyProperty = typeof(Study).GetProperties();
            foreach (var property in studyProperty)
            {
                CheckBox cb = new CheckBox();
                cb.Content = property.Name;
                cb.IsChecked = isContained("StudyColumnsToShow.txt", cb.Content.ToString());
                cb.Checked += (a, b) =>
                {
                    onChecked("StudyColumnsToShow.txt", cb.Content.ToString());
                };
                cb.Unchecked += (a, b) =>
                {
                    unCkecked("StudyColumnsToShow.txt", cb.Content.ToString());
                };
                studyPanel.Children.Add(cb);
                StudyProperties.Add(cb);
            }

            SeriesProperties = new List<CheckBox>();
            PropertyInfo[] seriesProperties = typeof(Series).GetProperties();
            foreach (var property in seriesProperties)
            {
                CheckBox cb = new CheckBox();
                cb.Content = property.Name;
                cb.IsChecked = isContained("SeriesColumnsToShow.txt", cb.Content.ToString()); ;
                cb.Checked += (a, b) =>
                {
                    onChecked("SeriesColumnsToShow.txt", cb.Content.ToString());
                };
                cb.Unchecked += (a, b) =>
                {
                    unCkecked("SeriesColumnsToShow.txt", cb.Content.ToString());
                };
                seriesPanel.Children.Add(cb);
                SeriesProperties.Add(cb);
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
                    configuration.thisNodePort = thisNodePort;
                    configuration.writeDown();
                    thisNodesName.Text = thisNodeAET + " " + thisNodePort;
                }
                popup.Close();
            };
            popup.AETTextBox.Text = configuration.thisNodeAET;
            popup.portTextBox.Text = configuration.thisNodePort;
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
                server ls = listView.SelectedItem as server;
                if (ls == null) ls = listView.Items[0] as server;
                currentServer.Text = ls.AET + "@" + ls.ip + ":" + ls.port;

                configuration.ip = ls.ip;
                configuration.port = ls.port;
                configuration.AET = ls.AET;
                configuration.writeDown();
            }
        }

        private void anonymizeDataCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            configuration.anonymizeData = bool.Parse(anonymizeDataCheckbox.IsChecked.ToString());
            configuration.writeDown();
        }
     
        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                destinationBox.Text= openFolderDialog.SelectedPath;

            if (configuration != null)
            {
                configuration.fileDestination = destinationBox.Text;
                configuration.writeDown();
            }
        }
    }
}
