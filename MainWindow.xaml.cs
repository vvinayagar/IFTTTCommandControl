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
         
            try
            {
                //userCredential = Login("Google ID");

                service2 = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = userCredential,
                });

             
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
                //tmrService.Start();
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

                string dPath = "";

                Dispatcher.Invoke(() =>
                {
                dPath= txtCommandPath.Text.Trim();

                });

                if (!string.IsNullOrEmpty(dPath)) {

                  string [] pathFiles =  Directory.GetFiles(dPath);

                    foreach (string path in pathFiles) {

                        if (path.Contains("command")) {

                            if(path.EndsWith(".gdoc"))
                            {
                                var json = SafeReadAllText(path); //System.IO.File.ReadAllText(path);
                                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                                string url = obj.url;
                                string fileId = obj.doc_id;
                                continue;
                            }


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


        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    // 1) Login & service
                    userCredential = await LoginAsync();
                    if (userCredential == null)
                    {
                        Dispatcher.Invoke(() => lsLogs.Items.Add("Auth failed."));
                        return;
                    }
                    service2 = BuildDriveService(userCredential);
                    Dispatcher.Invoke(() => lsLogs.Items.Add("Google Drive ready."));

                    // ---- A) PROCESS COMMANDS FROM GOOGLE DRIVE FOLDER ----
                    // Replace with your folderId
                    string driveFolderId = "1oJZBZWQf-BWAKWy4IgdQ4vR6_mZdpgYt";

                    var driveCmds = await GetCommandFilesInFolderAsync(driveFolderId);
                    foreach (var cmd in driveCmds)
                    {
                        if (string.IsNullOrWhiteSpace(cmd.Text)) continue;

                        string output;
                        try
                        {
                            DeleteFileAsync(cmd.Id).Wait();

                        }
                        catch { }


                        try { output = RunCommand(cmd.Text); }
                        catch (Exception exCmd) { output = "Command failed: " + exCmd.Message; }

                        Dispatcher.Invoke(() =>
                        {
                            if (lsLogs.Items.Count > 100) lsLogs.Items.Clear();
                            lsLogs.Items.Add($"[Drive] {cmd.Name}");
                            lsLogs.Items.Add(cmd.Text);
                            if (!string.IsNullOrWhiteSpace(output)) lsLogs.Items.Add(output);
                        });

                        // OPTIONAL: delete remote file after processing (requires Drive scope)
                        // await service2.Files.Delete(cmd.Id).ExecuteAsync();
                    }

                    // ---- B) (OPTIONAL) STILL PROCESS LOCAL COMMAND FILES ----
                    string dPath = "";
                    //Dispatcher.Invoke(() => dPath = txtCommandPath.Text.Trim());

                    //if (!string.IsNullOrEmpty(dPath) && Directory.Exists(dPath))
                    //{
                    //    string[] pathFiles = Directory.GetFiles(dPath);
                    //    foreach (string path in pathFiles)
                    //    {
                    //        var name = System.IO.Path.GetFileName(path);

                    //        // Only process "command*" files & skip temp
                    //        if (!name.StartsWith("command", StringComparison.OrdinalIgnoreCase)) continue;
                    //        if (name.StartsWith("~$") || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;

                    //        // Skip non-regular entries
                    //        FileAttributes attrs;
                    //        try { attrs = System.IO.File.GetAttributes(path); } catch { continue; }
                    //        if ((attrs & FileAttributes.Directory) != 0) continue;

                    //        string commandText = null;
                    //        string docFileId = null;
                    //        try
                    //        {
                    //            if (name.EndsWith(".gdoc", StringComparison.OrdinalIgnoreCase))
                    //            {
                    //                // Read .gdoc JSON safely
                    //                string json = SafeReadAllText(path);
                    //                if (string.IsNullOrWhiteSpace(json))
                    //                {
                    //                    Dispatcher.Invoke(() => lsLogs.Items.Add("Empty .gdoc: " + name));
                    //                    continue;
                    //                }

                    //                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    //                string fileId = null;
                    //                try { fileId = (string)obj.doc_id; } catch { }
                    //                if (string.IsNullOrWhiteSpace(fileId))
                    //                {
                    //                    string url = null;
                    //                    try { url = (string)obj.url; } catch { }
                    //                    fileId = ExtractIdFromUrl(url); // if you kept this helper
                    //                }
                    //                if (string.IsNullOrWhiteSpace(fileId))
                    //                {
                    //                    Dispatcher.Invoke(() => lsLogs.Items.Add("Cannot find fileId in .gdoc: " + name));
                    //                    continue;
                    //                }

                    //                commandText = await ExportGoogleDocAsTextAsync(fileId);
                    //            }
                    //            else
                    //            {
                    //                // Normal text command file
                    //                commandText = SafeReadAllText(path);
                    //            }
                    //        }
                    //        catch (Exception exRead)
                    //        {
                    //            Dispatcher.Invoke(() => lsLogs.Items.Add($"Read failed ({name}): {exRead.Message}"));
                    //            continue;
                    //        }

                         
                    //            Dispatcher.Invoke(() => lsLogs.Items.Add("Empty command: " + name));
                    //            try { 
                    //                System.IO.File.Delete(path); 
                    //                DeleteFileAsync(docFileId).Wait();

                    //            } catch { }
                      
                            

                    //        string output;
                    //        try { 
                                
                                
                    //            output = RunCommand(commandText); 
                            
                            
                    //        }
                    //        catch (Exception exCmd) { output = "Command failed: " + exCmd.Message; }

                    //        Dispatcher.Invoke(() =>
                    //        {
                    //            if (lsLogs.Items.Count > 100) lsLogs.Items.Clear();
                    //            lsLogs.Items.Add($"[Local] {name}");
                    //            lsLogs.Items.Add(commandText);
                    //            if (!string.IsNullOrWhiteSpace(output)) lsLogs.Items.Add(output);
                    //        });

                    //        try { System.IO.File.Delete(path); } catch { /* ignore */ }
                    //    }
                    //}
                    //else
                    //{
                    //    Dispatcher.Invoke(() => lsLogs.Items.Add("Local path skipped (empty or not found)."));
                    //}
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => lsLogs.Items.Add("Auth error: " + ex.Message));
                }
            });
        }
        private async Task TrashFileAsync(string fileId, CancellationToken ct = default(CancellationToken))
        {
            var body = new Google.Apis.Drive.v3.Data.File { Trashed = true };
            var upd = service2.Files.Update(body, fileId);
            upd.SupportsAllDrives = true;
            await upd.ExecuteAsync(ct);
        }

        private async Task DeleteFileAsync(string fileId, CancellationToken ct = default(CancellationToken))
        {
            var del = service2.Files.Delete(fileId);
            del.SupportsAllDrives = true;
            await del.ExecuteAsync(ct);
        }
        private async Task<File> ResolveShortcutAsync(File f, CancellationToken ct = default(CancellationToken))
        {
            if (!string.Equals(f.MimeType, "application/vnd.google-apps.shortcut", StringComparison.OrdinalIgnoreCase) ||
                f.ShortcutDetails == null || string.IsNullOrEmpty(f.ShortcutDetails.TargetId))
                return f;

            var get = service2.Files.Get(f.ShortcutDetails.TargetId);
            get.SupportsAllDrives = true;
            get.Fields = "id,name,mimeType";
            return await get.ExecuteAsync(ct);
        }

        // Download plain text OR export Google Docs → text
        private async Task<string> ReadDriveFileTextAsync(File f, CancellationToken ct = default(CancellationToken))
        {
            // Google Docs → export plain text
            if (string.Equals(f.MimeType, "application/vnd.google-apps.document", StringComparison.OrdinalIgnoreCase))
            {
                return await ExportGoogleDocAsTextAsync(f.Id, ct); // you already have this method
            }

            // Plain text (or other) → download and read as UTF-8
            var get = service2.Files.Get(f.Id);
            get.SupportsAllDrives = true;

            using (var ms = new MemoryStream())
            {
                await get.DownloadAsync(ms, ct);
                ms.Position = 0;
                using (var sr = new StreamReader(ms, Encoding.UTF8, true))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        // List files under a folder where the NAME starts with "command" (client-side filter)
        private async Task<IList<CommandFile>> GetCommandFilesInFolderAsync(string folderId, CancellationToken ct = default(CancellationToken))
        {
            var results = new List<CommandFile>();

            var req = service2.Files.List();
            req.Q = "'" + folderId + "' in parents and trashed = false and name contains 'command'";
            req.Fields = "nextPageToken, files(id,name,mimeType,shortcutDetails/targetId)";
            req.PageSize = 1000;
            req.SupportsAllDrives = true;
            req.IncludeItemsFromAllDrives = true;

            FileList page;
            do
            {
                page = await req.ExecuteAsync(ct);

                foreach (var f0 in page.Files)
                {
                    if (!f0.Name.StartsWith("command", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var f = await ResolveShortcutAsync(f0, ct);

                    string text = null;
                    try
                    {
                        text = await ReadDriveFileTextAsync(f, ct);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => lsLogs.Items.Add($"Read failed [{f.Name}]: {ex.Message}"));
                        continue;
                    }

                    results.Add(new CommandFile
                    {
                        Id = f.Id,
                        Name = f.Name,
                        MimeType = f.MimeType,
                        Text = text
                    });
                }

                req.PageToken = page.NextPageToken;
            }
            while (!string.IsNullOrEmpty(req.PageToken));

            return results;
        }

        private static string ExtractIdFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                url,
                @"https:\/\/docs\.google\.com\/document\/d\/([a-zA-Z0-9\-_]+)"
            );
            return (m.Success && m.Groups.Count > 1) ? m.Groups[1].Value : null;
        }


       


        private static bool IsCloudPlaceholder(string path)
        {
            try
            {
                var attrs =System.IO.File.GetAttributes(path);
                // ReparsePoint/Offline are common for cloud-files
                return (attrs & FileAttributes.ReparsePoint) != 0
                    || (attrs & FileAttributes.Offline) != 0;
            }
            catch { return false; }
        }

        private static bool WaitForStableFile(string path, int checks = 3, int delayMs = 150)
        {
            try
            {
                long last = -1;
                for (int i = 0; i < checks; i++)
                {
                    if (!System.IO.File.Exists(path)) return false;
                    var len = new FileInfo(path).Length;
                    if (len == 0) { Thread.Sleep(delayMs); continue; }
                    if (len == last) return true; // size stable twice
                    last = len;
                    Thread.Sleep(delayMs);
                }
            }
            catch { }
            return false;
        }

        //private static string SafeReadAllText(string path, int maxRetries = 8, int delayMs = 250)
        //{
        //    // HResult for "Incorrect function." - typically from a cloud file hydrating.
        //    const int ERROR_INVALID_FUNCTION = -2147024895; // 0x80070001

        //    for (int i = 0; i < maxRetries; i++)
        //    {
        //        try
        //        {
        //            // We go straight to reading the file.
        //            // FileShare.ReadWrite allows other processes (like Google Drive) to work on the file.
        //            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //            using (var sr = new StreamReader(fs, Encoding.UTF8, true))
        //            {
        //                return sr.ReadToEnd();
        //            }
        //        }
        //        catch (IOException ex) when (ex.HResult == ERROR_INVALID_FUNCTION)
        //        {
        //            // This is our specific hydration error.
        //            // Log it for debugging if needed, then wait and retry.
        //            if (i == maxRetries - 1)
        //            {
        //                // If it's the last attempt, re-throw the exception so the caller knows it failed.
        //                throw;
        //            }
        //            Thread.Sleep(delayMs * (i + 1)); // Back off a little each time
        //        }
        //        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        //        {
        //            // Catch other common file access errors (like sharing violations).
        //            if (i == maxRetries - 1)
        //            {
        //                throw;
        //            }
        //            Thread.Sleep(delayMs);
        //        }
        //    }
        //    return null; // Should not be reached if maxRetries > 0
        //}


        private static string SafeReadAllText(string path, int maxRetries = 6, int delayMs = 150)
        {
            // Skip obviously non-regular / not-ready entries early
            if (!System.IO.File.Exists(path)) return null;

            // For cloud placeholders, give them time to hydrate & stabilize
            if (IsCloudPlaceholder(path))
            {
                // Wait a bit for hydration; if not stable, bail out (return null)
                if (!WaitForStableFile(path, checks: 4, delayMs: delayMs))
                    return null;
            }

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // Try to read only after a quick stable-size check
                    if (!WaitForStableFile(path, checks: 2, delayMs: delayMs / 2))
                    {
                        Thread.Sleep(delayMs);
                    }

                    using (var fs = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete))
                    {
                        // Extra guard: if it’s still a weird handle, a small Read may fail;
                        // try a small probe first to trigger hydration
                        if (fs.Length > 0)
                        {
                            byte[] probe = new byte[Math.Min(64, (int)Math.Min(fs.Length, int.MaxValue))];
                            int read = fs.Read(probe, 0, probe.Length);
                            fs.Position = 0; // rewind
                        }

                        using (var sr = new StreamReader(fs, Encoding.UTF8, true))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                catch (IOException ex)
                {
                    // ERROR_INVALID_FUNCTION often shows up here on cloud placeholders
                    if (i == maxRetries - 1) throw; // let caller log exact error
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == maxRetries - 1) throw;
                    Thread.Sleep(delayMs);
                }
            }
            return null;
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

            try
            {
          

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

        /// <summary>
        /// Lists files under a folder and returns web links. Handles shortcuts by resolving targetId.
        /// </summary>
        private async Task<IList<DriveLinkInfo>> ListFolderLinksAsync(string folderId, CancellationToken ct = default(CancellationToken))
        {
            var results = new List<DriveLinkInfo>();

            var req = service2.Files.List();
            req.Q = "'" + folderId + "' in parents and trashed = false";
            req.Fields = "nextPageToken, files(id,name,mimeType,webViewLink,webContentLink,exportLinks,shortcutDetails/targetId)";
            req.PageSize = 1000;
            req.SupportsAllDrives = true;
            req.IncludeItemsFromAllDrives = true;

            FileList page = null;
            do
            {
                page = await req.ExecuteAsync(ct);

                foreach (var f in page.Files)
                {
                    // If this is a Drive shortcut, resolve to its target
                    if (string.Equals(f.MimeType, "application/vnd.google-apps.shortcut", StringComparison.OrdinalIgnoreCase)
                        && f.ShortcutDetails != null && !string.IsNullOrEmpty(f.ShortcutDetails.TargetId))
                    {
                        var getTarget = service2.Files.Get(f.ShortcutDetails.TargetId);
                        getTarget.Fields = "id,name,mimeType,webViewLink,webContentLink,exportLinks";
                        getTarget.SupportsAllDrives = true;

                        var target = await getTarget.ExecuteAsync(ct);
                        var infoT = new DriveLinkInfo
                        {
                            Id = target.Id,
                            Name = target.Name,
                            MimeType = target.MimeType,
                            WebViewLink = target.WebViewLink,
                            WebContentLink = target.WebContentLink,
                            ExportPlainTextLink = (target.ExportLinks != null && target.ExportLinks.ContainsKey("text/plain"))
                                                  ? target.ExportLinks["text/plain"]
                                                  : null
                        };
                        results.Add(infoT);
                        continue;
                    }

                    // Normal file or Google-native file
                    var info = new DriveLinkInfo
                    {
                        Id = f.Id,
                        Name = f.Name,
                        MimeType = f.MimeType,
                        WebViewLink = f.WebViewLink,       // open in browser UI
                        WebContentLink = f.WebContentLink, // direct download for binary files (if permitted)
                        ExportPlainTextLink = (f.ExportLinks != null && f.ExportLinks.ContainsKey("text/plain"))
                                              ? f.ExportLinks["text/plain"]
                                              : null
                    };
                    results.Add(info);
                }

                req.PageToken = page.NextPageToken;
            }
            while (!string.IsNullOrEmpty(req.PageToken));

            return results;
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

        private async Task<UserCredential> LoginAsync()
        {
            // Put your OAuth client file (from Google Cloud Console) next to the exe
            // File name typically: client_secret_<...>.json

            string credPath = "token.json";

            Dispatcher.Invoke(() => credPath = txtJsonPath.Text);

            if (!System.IO.File.Exists(credPath))
            {
                Dispatcher.Invoke(() => lsLogs.Items.Add("Credential file not found."));
                return null;
            
            }

            using (var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read))
            {
                var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

                // Ask only what you need. For read-only, use DriveReadonly
                var scopes = new[] { DriveService.Scope.Drive };

                // This will open a browser once and cache a token under %APPDATA%

                var result = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    secrets,
                    scopes,
                    "user",
                    CancellationToken.None
                );
                return result;
            }
        }

        private DriveService BuildDriveService(ICredential cred)
        {
            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "commandapp"
            });
        }

        private async Task<string> ExportGoogleDocAsTextAsync(string fileId, CancellationToken ct = default)
        {
            var request = service2.Files.Export(fileId, "text/plain");

            using (var ms = new MemoryStream())
            {
                await request.DownloadAsync(ms, ct);
                ms.Position = 0;

                using (var reader = new StreamReader(ms, Encoding.UTF8, true))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private static string RunCommand(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        private void btnJsonPath_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtJsonPath_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
