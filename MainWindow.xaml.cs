using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Google.Apis.Drive.v3.FilesResource;
using File = Google.Apis.Drive.v3.Data.File;

namespace CommandControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UserCredential userCredential;
        DriveService service2;
        bool bool_tmr = false;
        NotifyIcon trayIcon;
        private NotifyIcon _trayIcon;
        System.Timers.Timer tmrService;
        private bool _isExitRequested = false;
        public MainWindow()
        {
            InitializeComponent();

            //Load settings
            txtCommandPath.Text = Properties.Settings.Default.CommandPath;
            checkStartInStartup.IsChecked = Properties.Settings.Default.StartInStartup;


            _trayIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("favicon.ico"), // or new Icon("app.ico");
                Visible = true,
                Text = "Tray App (double-click to restore)"
            };

            // Double-click to restore
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            // Right-click menu
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => RestoreFromTray());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            _trayIcon.ContextMenuStrip = menu;


            this.WindowState = WindowState.Minimized;
            Hide();
            //trayIcon = new NotifyIcon();
            //trayIcon.Icon = new System.Drawing.Icon("favicon.ico");
            //trayIcon.Visible = true;
            //trayIcon.DoubleClick += (s, e) =>
            //{
            //    this.Show();
            //    this.WindowState = WindowState.Normal;
            //};
            //GoogleCredential credential = GoogleCredential.FromJson(System.IO.File.ReadAllText("mycommand-413502-78d4df28efc0.json"));

            try
            {
                //userCredential = Login("Google ID");

                service2 = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                });

                //var filesRequest = service2.Files.List();
                //filesRequest.Q = "parents in '1oJZBZWQf-BWAKWy4IgdQ4vR6_mZdpgYt'";

                //var pageStreamer = new PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
                //requestModifier: (req, token) => {
                //    filesRequest.PageToken = token;
                //},
                //tokenExtractor: (res) => res.NextPageToken,
                //resourceExtractor: (res1) => {
                //    var fls = res1.Files;
                //    return res1.Files;
                //});


                //var all = new FileList();
                //all.Files = new List<Google.Apis.Drive.v3.Data.File>();

                //foreach (var result in await pageStreamer.FetchAllAsync(filesRequest, CancellationToken.None))
                //{
                //    all.Files.Add(result);
                //}

                //foreach (var item in all.Files)
                //{
                //    string filename = item.Name;
                //    lsLogs.Items.Add(filename);
                //    File file = new File();
                //    file.Name = "Updated.png";
                //    FilesResource.UpdateRequest updateRequest = service2.Files.Update(file, item.Id);
                //    updateRequest.Execute();

                //    if (filename.Contains("shutdown"))
                //    {
                //        MessageBox.Show("Shutdown PC");
                //    }
                //}

            }
            catch (Exception ex)
            {
                lsLogs.Items.Add($"{ex.Message}");
            }


            tmrService = new System.Timers.Timer();
            tmrService.Interval = 1000;
            tmrService.Elapsed += TmrService_Elapsed;



            // if start in startup checked then run the timer on startup
            if (checkStartInStartup.IsChecked == true)
            {
                bool_tmr = true;
                btnTimer.Content = "Stop Timer";
                tmrService.Start();
            }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            _isExitRequested = true;
            Close();
        }

        private async void TmrService_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {

                //var filesRequest = service2.Files.List();
                //filesRequest.Q = "parents in '1oJZBZWQf-BWAKWy4IgdQ4vR6_mZdpgYt'";

                //var pageStreamer = new PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
                //requestModifier: (req, token) =>
                //{
                //    filesRequest.PageToken = token;
                //},
                //tokenExtractor: (res) => res.NextPageToken,
                //resourceExtractor: (res1) =>
                //{
                //    var fls = res1.Files;
                //    return res1.Files;
                //});


                //var all = new FileList();
                //all.Files = new List<Google.Apis.Drive.v3.Data.File>();

                //foreach (var result in await pageStreamer.FetchAllAsync(filesRequest, CancellationToken.None))
                //{
                //    all.Files.Add(result);
                //}

                //foreach (var item in all.Files)
                //{
                //    string filename = item.Name;
                //    Dispatcher.Invoke(() =>
                //    {
                //        if (lsLogs.Items.Count > 100)
                //        {
                //            lsLogs.Items.Clear();
                //        }
                //        lsLogs.Items.Add(filename);
                //    });

                //    File file = new File();
                //    file.Name = "Updated.png";
                //    FilesResource.UpdateRequest updateRequest = service2.Files.Update(file, item.Id);
                //    updateRequest.Execute();

                //    if (filename.Contains("shutdown"))
                //    {
                //        System.Windows.MessageBox.Show("Shutdown PC");
                //    }
                //}

                string dPath = "";

                Dispatcher.Invoke(() =>
                {
                dPath= txtCommandPath.Text.Trim();

                });

                if (!string.IsNullOrEmpty(dPath)) {

                  string [] pathFiles =  Directory.GetFiles(dPath);

                    foreach (string path in pathFiles) {

                        if (path.Contains("command")) {

                            string content = System.IO.File.ReadAllText(path);

                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = "cmd.exe";
                            psi.Arguments = "/c " + content; // command here
                            psi.RedirectStandardOutput = true;
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;

                            Process process = Process.Start(psi);

                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            Console.WriteLine(output);

                            //lsLogs.Items.Add(content);
                            Dispatcher.Invoke(() =>
                            {
                                lsLogs.Items.Add($"{content}");
                            });

                            System.IO.File.Delete(path);

                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    lsLogs.Items.Add($"{ex.Message}");
                });

            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {

        }
        public static MemoryStream DriveDownloadFile(string fileId)
        {
            try
            {

                string credentialPath = "mycommand-413502-78d4df28efc0.json";

                /* Load pre-authorized user credentials from the environment.
                 TODO(developer) - See https://developers.google.com/identity for 
                 guides on implementing OAuth2 for your application. */
                //GoogleCredential credential = null;

                //using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                //{

                //}credential = GoogleCredential.FromJ

                //= GoogleCredential
                //.GetApplicationDefault()
                //.CreateScoped(DriveService.Scope.Drive);

                GoogleCredential credential = GoogleCredential.FromFile("mycommand-413502-78d4df28efc0.json");

                // Create Drive API service.
                var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "commandapp"
                });

                service.Files.List();

                var request = service.Files.Get(fileId);
                var stream = new MemoryStream();

                // Add a handler which will be notified on progress changes.
                // It will notify on each chunk download and when the
                // download is completed or failed.
                request.MediaDownloader.ProgressChanged +=
                    progress =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    Console.WriteLine(progress.BytesDownloaded);
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    Console.WriteLine("Download complete.");
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    Console.WriteLine("Download failed.");
                                    break;
                                }
                        }
                    };
                request.Download(stream);

                return stream;
            }
            catch (Exception e)
            {
                // TODO(developer) - handle error appropriately
                if (e is AggregateException)
                {
                    Console.WriteLine("Credential Not found");
                }
                else
                {
                    throw;
                }
            }
            return null;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {

            //try
            //{
            //    //string filecontent = await Fi

            //    GoogleCredential credential = GoogleCredential.
            //        FromFile("mycommand-413502-78d4df28efc0.json").CreateScoped(DriveService.Scope.Drive);
            //    var service = new DriveService(new BaseClientService.Initializer
            //    {
            //        HttpClientInitializer = credential,
            //        //ApplicationName = "commandapp",
            //        ApiKey = "78d4df28efc0fefa77ab501316c4624065538aa8"
            //    });

            //    lsLogs.Items.Add("API : " + service.ApiKey);

            //    var filesRequest = service.Files.List();
            //    filesRequest.Q = "parents in '1US7OjhPb4vJBZWeDYv9YMaFuDcDMTOQz'";

            //    var pageStreamer = new PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
            //    requestModifier: (req, token) => {
            //        filesRequest.PageToken = token;
            //    },
            //    tokenExtractor: (res) => res.NextPageToken,
            //    resourceExtractor: (res1) => { 
            //        var fls = res1.Files;
            //        return res1.Files; });


            //    var all = new FileList();
            //    all.Files = new List<Google.Apis.Drive.v3.Data.File>();

            //    var result2 = await pageStreamer.FetchAllAsync(filesRequest, CancellationToken.None);

            //    foreach (var result in await pageStreamer.FetchAllAsync(filesRequest, CancellationToken.None))
            //    {
            //        all.Files.Add(result);
            //    }


            //    foreach (var item in all.Files)
            //    {
            //        lsLogs.Items.Add(item.Name);
            //    }

            //    //foreach (var item in filesRequest.Fields)
            //    //{
            //    //    lsLogs.Items.Add(item);
            //    //}


            //    var drives = service.Drives.List();
            //    drives.Q = "parents in '1oJZBZWQf-BWAKWy4IgdQ4vR6_mZdpgYt'";


            //    var pageStreamer2 = new PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
            //    requestModifier: (req, token) => drives.PageToken = token,
            //    tokenExtractor: (res) => res.NextPageToken,
            //    resourceExtractor: (res1) => res1.Files);

            //    foreach (var result in await pageStreamer2.FetchAllAsync(filesRequest, CancellationToken.None))
            //    {
            //        all.Files.Add(result);
            //    }

            //       foreach (var item in all.Files)
            //    {
            //        lsLogs.Items.Add(item.Name);
            //    }

            //}
            //catch (Exception ex) {

            //    lsLogs.Items.Add($"{ex.Message}");
            //}


            try
            {
                //userCredential = Login("990038621125-bv4d35omc54hoc51d87fcnite8qlggeu.apps.googleusercontent.com",
                //              "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");

                //var service2 = new DriveService(new BaseClientService.Initializer
                //{
                //    HttpClientInitializer = userCredential,
                //    //ApplicationName = "commandapp",

                //});

                var filesRequest = service2.Files.List();
                filesRequest.Q = "parents in '1oJZBZWQf-BWAKWy4IgdQ4vR6_mZdpgYt'";

                var pageStreamer = new PageStreamer<Google.Apis.Drive.v3.Data.File, FilesResource.ListRequest, Google.Apis.Drive.v3.Data.FileList, string>(
                requestModifier: (req, token) =>
                {
                    filesRequest.PageToken = token;
                },
                tokenExtractor: (res) => res.NextPageToken,
                resourceExtractor: (res1) =>
                {
                    var fls = res1.Files;
                    return res1.Files;
                });


                var all = new FileList();
                all.Files = new List<Google.Apis.Drive.v3.Data.File>();

                foreach (var result in await pageStreamer.FetchAllAsync(filesRequest, CancellationToken.None))
                {
                    all.Files.Add(result);
                }

                foreach (var item in all.Files)
                {
                    string filename = item.Name;
                    lsLogs.Items.Add(filename);
                    File file = new File();
                    file.Name = "Updated.png";
                    FilesResource.UpdateRequest updateRequest = service2.Files.Update(file, item.Id);
                    updateRequest.Execute();

                    if (filename.Contains("shutdown"))
                    {
                        System.Windows.MessageBox.Show("Shutdown PC");
                    }
                }

            }
            catch (Exception ex)
            {
                lsLogs.Items.Add($"{ex.Message}");
            }


        }


        private static UserCredential Login(string googleClientId, string googleClientSecret)
        {
            ClientSecrets clientSecrets = new ClientSecrets();
            {
                clientSecrets.ClientId = googleClientId;
                clientSecrets.ClientSecret = googleClientSecret;
            }

            return GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecrets, new string[] { DriveService.Scope.Drive }, "user", CancellationToken.None).Result;
        }

        private void btnTimer_Click(object sender, RoutedEventArgs e)
        {
            if (bool_tmr)
            {
                btnTimer.Content = "Start Timer";
                bool_tmr = false;
                tmrService.Stop();
            }
            else
            {
                btnTimer.Content = "Stop Timer";
                bool_tmr = true;
                tmrService.Start();
            }
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {


            FolderBrowserDialog fileDialog = new FolderBrowserDialog();
 

            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = fileDialog.SelectedPath;
                lsLogs.Items.Add(path);

                txtCommandPath.Text = path;

                //Save to settings
                Properties.Settings.Default.CommandPath = path;
                Properties.Settings.Default.Save();
            }


        }

        private void checkStartInStartup_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StartInStartup = checkStartInStartup.IsChecked == true;
            Properties.Settings.Default.Save();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {

            // When user clicks minimize button, hide window & keep in tray
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExitRequested)
            {
                // Intercept close (X button) → minimize to tray instead
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                return;
            }

            // real exit: cleanup
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            base.OnClosing(e);
        }
    }
}
