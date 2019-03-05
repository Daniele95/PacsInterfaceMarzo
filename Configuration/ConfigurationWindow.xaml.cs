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
        public string ip { get; private set; }
        public string port { get; private set; }
        public string AET { get; private set; }
        public bool saveData { get; private set; }
        public string thisNodeAET { get; private set; }
        public string thisNodePort { get; private set; }

        public CurrentConfiguration()
        {
            update();
        }

        public void update()
        {
            ip = File.ReadAllLines("ServerConfig.txt")[0];
            port = File.ReadAllLines("ServerConfig.txt")[1];
            AET = File.ReadAllLines("ServerConfig.txt")[2];
            saveData = bool.Parse(File.ReadAllLines("ServerConfig.txt")[3]);
            thisNodeAET = File.ReadAllLines("ServerConfig.txt")[4];
            thisNodePort = File.ReadAllLines("ServerConfig.txt")[5];
        }

        public void setIp(string newIp)
        {
            ip = newIp;
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[0] = newIp;
            File.WriteAllLines("ServerConfig.txt", lines);
        }
        public void setPort(string newPort)
        {
            port = newPort;
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[1] = newPort;
            File.WriteAllLines("ServerConfig.txt", lines);
        }
        public void setAET(string newAET)
        {
            AET = newAET;
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[2] = newAET;
            File.WriteAllLines("ServerConfig.txt", lines);
        }
        public void setSaveData(string newSaveData)
        {
            saveData = bool.Parse(newSaveData);
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[3] = newSaveData;
            File.WriteAllLines("ServerConfig.txt", lines);
        }
        public void setThisNodeAET(string newThisNodeAET)
        {
            thisNodeAET = newThisNodeAET;
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[4] = newThisNodeAET;
            File.WriteAllLines("ServerConfig.txt", lines);
        }
        public void setThisNodePort(string newThisNodePort)
        {
            thisNodePort = newThisNodePort;
            string[] lines = File.ReadAllLines("ServerConfig.txt");
            lines[5] = newThisNodePort;
            File.WriteAllLines("ServerConfig.txt", lines);
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
        public class StudyQueryOut
        {
            public string StudyInstanceUID { get; set; } = "";
            public string PatientID { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string StudyDate { get; set; } = "";
            public string ModalitiesInStudy { get; set; } = "";
            public string PatientBirthDate { get; set; } = "";
            public string StudyDescription { get; set; } = "";
        }
        public class SeriesQueryOut
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
            if (isContained(file, content))
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

            saveDataCheckbox.IsChecked = configuration.saveData;
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
            PropertyInfo[] studyProperty = typeof(StudyQueryOut).GetProperties();
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
            PropertyInfo[] seriesProperties = typeof(SeriesQueryOut).GetProperties();
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
                    configuration.setThisNodeAET(thisNodeAET);
                    configuration.setThisNodePort(thisNodePort);
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

                configuration.setIp(ls.ip);
                configuration.setPort(ls.port);
                configuration.setAET(ls.AET);
            }
        }

        private void saveDataCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (listView.Items.Count != 0)
            {
                server ls = listView.SelectedItem as server;
                if (ls == null) ls = listView.Items[0] as server;

                configuration.setSaveData(saveDataCheckbox.IsChecked.ToString());
            }
        }

    }
}
