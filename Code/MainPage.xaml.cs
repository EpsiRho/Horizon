using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;


namespace Horizon
{
    public class openingChecks
    {
        public static bool ContactsTip;
        public static bool IPTip;
        public static bool ServerToggleTip;
        public static bool ServerClosedTip;
    }
    public sealed partial class MainPage : Page
    {
        // Global Vars
        Thread beaconListen;
        Thread SocketQueue;
        string AccentColor;
        class SocketTracker
        {
            public string[] args;
            public Socket sock;
            public string RQ;
            public SocketTracker(string[] args, Socket sock, string RQ)
            {
                this.args = args;
                this.sock = sock;
                this.RQ = RQ;
            }
        }
        List<SocketTracker> socketTrackers = new List<SocketTracker>();
        public ContactsViewModel ViewModel { get; set; }
        public ConnectionsViewModel connectionsViewModel { get; set; }
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        bool ReadyForQueue;
        bool QueueActive;
        bool CancelBool;
        bool ipset;
        string PublicIP;
        string UnkownIpAllow;
        int ConnectionTimeout;
        int AcceptTimeout;

        // Page Load
        public MainPage()
        {
            GetSettings();
            this.InitializeComponent();
            OnLoad();
        }
        public void OnLoad()
        {
            beaconListen = new Thread(new ThreadStart(BeaconListen));
            beaconListen.Name = "Beacon";
            SocketQueue = new Thread(new ThreadStart(RequestQueue));
            this.ViewModel = new ContactsViewModel();
            this.connectionsViewModel = new ConnectionsViewModel();
            Thread chkServerStatus = new Thread(delegate ()
            {
                _ = CheckIfServerActiveAsync();
            });
            chkServerStatus.IsBackground = true;
            chkServerStatus.Name = "chkServerStatus";
            chkServerStatus.Start();
            ipset = false;
            if (openingChecks.ContactsTip == true)
            {
                AddContactButtonTip.IsOpen = true;
            }
            else if (openingChecks.ServerToggleTip == true)
            {
                ServerToggleTip.IsOpen = true;
            }
            getIP();
        }
        public void GetSettings()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            AccentColor = (string)localSettings.Values["Accent"];
            if (AccentColor == null)
            {
                localSettings.Values["Accent"] = "#FF1EAED8";
                AccentColor = (string)localSettings.Values["Accent"];
            }
            UnkownIpAllow = (string)localSettings.Values["UnkownIpAllow"];
            if (UnkownIpAllow == null)
            {
                localSettings.Values["UnkownIpAllow"] = "false";
                UnkownIpAllow = (string)localSettings.Values["UnkownIpAllow"];
            }
            try
            {
                ConnectionTimeout = (Int32)localSettings.Values["ConnectionTimeout"];
            }
            catch (Exception)
            {
                localSettings.Values["ConnectionTimeout"] = 15000;
                ConnectionTimeout = (Int32)localSettings.Values["ConnectionTimeout"];
            }
            try
            {
                AcceptTimeout = (Int32)localSettings.Values["AcceptTimeout"];
            }
            catch (Exception)
            {
                localSettings.Values["AcceptTimeout"] = 60000;
                AcceptTimeout = (Int32)localSettings.Values["AcceptTimeout"];
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Thread ListViewPopulate = new Thread(new ThreadStart(PullFromDatabase));
            ListViewPopulate.Start();
        }

        // Information Functions
        public async void PullFromDatabase()
        {
            List<string> NameList = ContactsAccess.GetNames();
            List<string> IPList = ContactsAccess.GetIPs();

            for (int i = 0; i < NameList.Count; i++)
            {
                bool insert = false;
                for (int j = 0; j < ViewModel.Contacts.Count; j++)
                {
                    if (string.Compare(ViewModel.Contacts[j].Name, NameList[i]) > 0)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ViewModel.InsetAt(j, new Contact(NameList[i], IPList[i]));
                        });
                        insert = true;
                        break;
                    }
                }
                if (insert == false)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ViewModel.AddContact(NameList[i], IPList[i]);
                    });
                }
            }
        }
        public async void SortName(object item)
        {
            Contact contact = (Contact)item;
            bool insert = false;
            for (int j = 0; j < ViewModel.Contacts.Count; j++)
            {
                if (string.Compare(ViewModel.Contacts[j].Name, contact.Name) > 0)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ViewModel.InsetAt(j, contact);
                    });
                    insert = true;
                    break;
                }
            }
            if (insert == false)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ViewModel.AddContact(contact.Name, contact.IP);
                });
            }
        }
        public async System.Threading.Tasks.Task CheckIfServerActiveAsync()
        {
            bool isHover = false;
            while (true)
            {
                try
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        isHover = ServerToggle.IsPointerOver;
                    });
                    if (beaconListen.IsAlive && !isHover)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ServerToggle.Background = new SolidColorBrush(Color.FromArgb(255, 0, 140, 0));
                        });
                    }
                    else if (!isHover)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ServerToggle.Background = new SolidColorBrush(Color.FromArgb(255, 140, 0, 0));
                        });
                    }
                    Thread.Sleep(50);
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                }
            }
        }
        public void SendToastNotification(string[] args)
        {
             var content = new ToastContentBuilder()
                .AddText(ViewModel.searchByIp(args[1]) + " is sending you a file", hintMaxLines: 1)
                .AddText(args[2])
                .AddText(args[3])
                .GetToastContent();

            // Create the notification
            var notif = new ToastNotification(content.GetXml());

            // And show it!
            ToastNotificationManager.CreateToastNotifier().Show(notif);
        }
        private async void showIP()
        {
            if (!ipset)
            {
                getIP();
                ipset = true;
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (ShowIPToolTip.IsOpen)
                {
                    ShowIPToolTip.IsOpen = false;
                }
                else
                {
                    ShowIPToolTip.Subtitle = PublicIP;
                    ShowIPToolTip.IsOpen = true;
                }
            });
        }
        private void getIP()
        {
            try
            {
                string htmldownload = new WebClient().DownloadString("http://checkip.dyndns.org");
                string[] split = htmldownload.Split(':');
                string substr = split[1].Substring(1);
                string[] substrsplit = substr.Split('<');
                PublicIP = substrsplit[0];
            }
            catch(Exception)
            {
                getIP();
            }
        }
        public async void InfoPopup(string title, string subtitle, Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode placement = Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.Top, object target = null)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                InformationTip.Title = title;
                InformationTip.Subtitle = subtitle;
                InformationTip.PreferredPlacement = placement;
                InformationTip.Target = target as FrameworkElement;
                InformationTip.IsOpen = true;
            });
        }
        public async void WriteLog(string Log)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile logFile = await localFolder.CreateFileAsync("Logs.txt",
                CreationCollisionOption.OpenIfExists);
            var stream = await logFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(stream.Size))
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                {
                    byte[] fileBuffer = Encoding.ASCII.GetBytes(Convert.ToString(Log));
                    dataWriter.WriteBytes(fileBuffer);
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            stream.Dispose();
        }
        public async void HideInfoPopup()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                InformationTip.IsOpen = false;
                ShowIPToolTip.IsOpen = false;
            });
        }

        // Buttons
        // ---------------------------- Top Bar ---------------------------- //
        private void ShowIPButton_Click(object sender, RoutedEventArgs e)
        {
            HideInfoPopup();
            IPButtonTip.IsOpen = false;
            Thread showipasync = new Thread(new ThreadStart(showIP));
            showipasync.Start();
        }
        private void ServerToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideInfoPopup();
                ServerToggleTip.IsOpen = false;
                if (beaconListen.IsAlive && SocketQueue.IsAlive)
                {
                    QueueActive = false;
                    allDone.Set();
                    beaconListen.Join();
                    SocketQueue.Join();
                    //InfoPopup("Server Status", "Server Offline", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.Bottom, ServerToggle);
                }
                else
                {
                    QueueActive = true;
                    beaconListen = new Thread(new ThreadStart(BeaconListen));
                    beaconListen.Start();
                    SocketQueue = new Thread(new ThreadStart(RequestQueue));
                    SocketQueue.Start();
                    if (beaconListen.IsAlive && SocketQueue.IsAlive)
                    {
                        //InfoPopup("Server Status", "Server Online", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.Bottom, ServerToggle);
                    }
                    else
                    {
                        InfoPopup("Server Error", "Server Couldn't start", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.Bottom, ServerToggle);
                        WriteLog("[Server Toggle Failure] - Server Couldn't Start\n");
                    }
                }
            }
            catch (Exception error)
            {
                InfoPopup("System Failure", error.Message + "||" + error.StackTrace + "||" + error.TargetSite);
                WriteLog("[Server Toggle Failure] - " + error.Message + "\n");
                return;
            }
        }
        private async void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            HideInfoPopup();
            AddContactButtonTip.IsOpen = false;
            await AddContactDialog.ShowAsync();
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HideInfoPopup();
            if (beaconListen.IsAlive && SocketQueue.IsAlive)
            {
                InfoPopup("Can't open Help", "Please shut down the server first", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.BottomLeft, HelpButton);
            }
            else if (connectionsViewModel.Connections.Count > 0)
            {
                InfoPopup("Can't open Help", "Please remove files from queue first", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.BottomLeft, HelpButton);
            }
            else
            {
                this.Frame.Navigate(typeof(Help));
                openingChecks.ContactsTip = false;
                openingChecks.IPTip = false;
                openingChecks.ServerToggleTip = false;
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            HideInfoPopup();
            if (beaconListen.IsAlive && SocketQueue.IsAlive)
            {
                InfoPopup("Can't open Settings", "Please shut down the server first", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.BottomLeft, SettingsButton);
            }
            else if (connectionsViewModel.Connections.Count > 0)
            {
                InfoPopup("Can't open Settings", "Please remove files from queue first", Microsoft.UI.Xaml.Controls.TeachingTipPlacementMode.BottomLeft, SettingsButton);
            }
            else
            {
                this.Frame.Navigate(typeof(Settings));
                openingChecks.ContactsTip = false;
                openingChecks.IPTip = false;
                openingChecks.ServerToggleTip = false;
            }
        }

        // ---------------------------- Contacts Area ---------------------------- //
        private async void SendFileFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Define and show the file picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            //IRandomAccessStream filestream = await file.OpenAsync(FileAccessMode.ReadWrite);

            if (file != null) // If the file is valid
            {
                Thread fileHandler = new Thread(FileSendRequestHandler);
                fileHandler.Start(file);
            }
            else
            {
                InfoPopup("File Error", "File Access Denied");
                WriteLog("[Filesystem] - Access Denied\n");
            }
        }
        private void ReceiveFileFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Thread fileHandler = new Thread(FileReceiveRequestHandler);
            fileHandler.Start();
        }
        public void DeleteContact_Click(object sender, RoutedEventArgs e)
        {
            Contact item = (Contact)ContactsListView.SelectedItem;
            ContactsAccess.DeleteData(item.Name);
            ViewModel.RemoveContact(item);
        }
        private void ContactsListView_Tapped(object sender, TappedRoutedEventArgs e) 
        { 
            HideInfoPopup();
        }
        private void ContactStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element != null)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }
        private void ConnectionsView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            HideInfoPopup();
            FrameworkElement element = sender as FrameworkElement;
            if (element != null)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }
        private void CancelMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Connection ListItem = (Connection)ConnectionsView.SelectedItem;
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Set();
            }
            catch (Exception)
            {

            }
        }

        // ---------------------------- Dialog Buttons ---------------------------- //
        private void AddContactDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ErrorText.Text = "";
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                args.Cancel = true;
                ErrorText.Text += "Name is required\n";
            }
            
            if(string.IsNullOrWhiteSpace(IPInput.Text))
            {
                args.Cancel = true;
                ErrorText.Text += "Ip is required\n";
            }

            List<string> names = ContactsAccess.GetNames();
            bool alreadyTaken = false;
            foreach (string name in names)
            {
                if (NameInput.Text == name)
                {
                    alreadyTaken = true;
                    break;
                }
            }
            if (alreadyTaken == true)
            {
                args.Cancel = true;
                ErrorText.Text += "Name has been taken";
            }

            if (ErrorText.Text == "")
            {
                ContactsAccess.AddData(NameInput.Text, IPInput.Text);

                if (ViewModel.Contacts.Count == 0)
                {
                    ViewModel.AddContact(NameInput.Text, IPInput.Text);
                    return;
                }
                Thread SortNewName = new Thread(SortName);
                SortNewName.Start(new Contact(NameInput.Text, IPInput.Text));
            }
        }
        private void AcceptFile_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            byte[] messageSent = Encoding.ASCII.GetBytes("HRZNACCEPT");
            int byteSent = socketTrackers[0].sock.Send(messageSent);

            Thread downloadThread = new Thread(downloadFromClientAsync);
            downloadThread.Start(socketTrackers[0]);
            CancelBool = false;
        }
        private void AcceptFile_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            byte[] messageSent = Encoding.ASCII.GetBytes("HRZNDENY");
            int byteSent = socketTrackers[0].sock.Send(messageSent);

            socketTrackers[0].sock.Close();
            socketTrackers.RemoveAt(0);
            ReadyForQueue = true;
            CancelBool = false;
        }
        private void AddContactButtonTip_CloseButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            IPButtonTip.IsOpen = true;
        }
        private async void RequestFile_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Define and show the file picker
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            //IRandomAccessStream filestream = await file.OpenAsync(FileAccessMode.ReadWrite);

            if (file != null) // If the file is valid
            {
                Thread fileHandler = new Thread(FileSendToRequestingHandler);
                fileHandler.Start(file);
            }
            else
            {
                InfoPopup("File Error", "File Access Denied");
                WriteLog("[Filesystem] - Access Denied\n");
            }
        }
        private void RequestFile_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            byte[] messageSent = Encoding.ASCII.GetBytes("HRZNDENY");
            int byteSent = socketTrackers[0].sock.Send(messageSent);

            socketTrackers[0].sock.Close();
            socketTrackers.RemoveAt(0);
            ReadyForQueue = true;
            CancelBool = false;
        }

        // Server Functions
        public void BeaconListen()
        {
            // Grabs local ip @ port 47
            IPAddress ipAddr = IPAddress.Parse("0.0.0.0");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 47);

            // Create a socket with TCP
            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Bind the listener socket
                listener.Bind(localEndPoint);

                // Listen for incoming traffic
                listener.Listen(10);
                try
                {
                    while (QueueActive)
                    {
                        // Set the event to nonsignaled state.  
                        allDone.Reset();

                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }
                }
                catch (Exception)
                {
                    listener.Close();
                    return;
                }
                listener.Close();
            }
            catch (Exception error)
            {
                InfoPopup("Beacon Error", error.Message);
                WriteLog("[Beacon Failure] - " + error.Message + "\n");
                listener.Close();
                return;
            }
        }
        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                allDone.Set();

                // Get the socket that handles the client request.  
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Get the request
                byte[] buffer = new byte[1024];
                handler.Receive(buffer);

                string request = new string(Encoding.ASCII.GetChars(buffer));
                request = request.Replace("\0", string.Empty);

                if (request.Contains("HRZNRQ"))
                {
                    string[] split = request.Split("/");
                    if (split.Length == 4)
                    {
                        if (ViewModel.searchByIp(split[1]).Contains("Unknown") && UnkownIpAllow == "false")
                        {
                            handler.Close();
                            return;
                        }
                        SocketTracker scktkr = new SocketTracker(split, handler, "SEND");
                        socketTrackers.Add(scktkr);
                    }
                    else if(split.Length == 3)
                    {
                        if (ViewModel.searchByIp(split[1]).Contains("Unknown") && UnkownIpAllow == "false")
                        {
                            handler.Close();
                            return;
                        }
                        if (split[1] == "Recv")
                        {
                            SocketTracker scktkr = new SocketTracker(split, handler, "RECV");
                            socketTrackers.Add(scktkr);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

        }
        public async void CancelCheck(object data)
        {
            try
            {
                Socket sock = (Socket)data;

                while (CancelBool)
                {
                    if (sock.Poll(1000, SelectMode.SelectRead) && (sock.Available == 0) || !sock.Connected)// First Check
                    {
                        if (sock.Poll(1000, SelectMode.SelectRead) && (sock.Available == 0) || !sock.Connected)// Second Check incase of Poll/Availble Desync
                        {
                            CancelBool = false;
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                AcceptFile.Hide();
                            });
                            ReadyForQueue = true;
                            socketTrackers[0].sock.Close();
                            socketTrackers.RemoveAt(0);
                        }
                    }
                }
                
            }
            catch (Exception)
            {
                return;
            }

        }
        public void RequestQueue()
        {
            try
            {
                ReadyForQueue = true;
                while (QueueActive)
                {
                    if(socketTrackers.Count > 0 && ReadyForQueue == true)
                    {
                        try{
                            if (socketTrackers[0].RQ == "SEND")
                            {
                                WaitForSendFileAccept(socketTrackers[0].args, socketTrackers[0].sock);
                                ReadyForQueue = false;
                            }
                            else if(socketTrackers[0].RQ == "RECV")
                            {
                                WaitForRecvFileAccept(socketTrackers[0].args, socketTrackers[0].sock);
                                ReadyForQueue = false;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception error)
            {
                beaconListen.Interrupt();
                beaconListen.Join();
                InfoPopup("Queue Error", "Server Shutdown");
                WriteLog("[Queue Failure] - " + error.Message + "\n");
                socketTrackers[0].sock.Close();
                socketTrackers.Clear();
                QueueActive = false;
                return;
            }
        }
        public async System.Threading.Tasks.Task WaitForSendFileAccept(string[] split, Socket sock)
        {
            string name = ViewModel.searchByIp(split[1]);
            if(name == null)
            {
                name = split[1];
            }
            //SendToastNotification(split);
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AcceptFile.Title = name + " is sending you a file";
                    SentFileName.Text = split[2];
                    SentFileSize.Text = split[3];
                    AcceptFile.ShowAsync();
                });

                // cancel check
                CancelBool = true;
                Thread cancelCheck = new Thread(CancelCheck);
                cancelCheck.Start(sock);
            }
            catch (Exception error)
            {
                WriteLog("[FileAcceptPopup Failure] - " + error.Message + "\n");
                socketTrackers[0].sock.Close();
                socketTrackers.RemoveAt(0);
                return;
            }
        }
        public async System.Threading.Tasks.Task WaitForRecvFileAccept(string[] split, Socket sock)
        {
            string name = ViewModel.searchByIp(split[2]);
            if (name == null)
            {
                name = split[2];
            }
            //SendToastNotification(split);
            try
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    RequestFile.Title = name + " is Requesting a File";
                    RequestFile.ShowAsync();
                });

                // cancel check
                CancelBool = true;
                Thread cancelCheck = new Thread(CancelCheck);
                cancelCheck.Start(sock);
            }
            catch (Exception error)
            {
                WriteLog("[FileAcceptPopup Failure] - " + error.Message + "\n");
                socketTrackers[0].sock.Close();
                socketTrackers.RemoveAt(0);
                return;
            }
        }
        public async void downloadFromClientAsync(object tracker)
        {
            try
            {
                SocketTracker info = (SocketTracker)tracker;
                Socket sock = info.sock;
                string filename = info.args[2];

                byte[] sizeBuf = new byte[1024];
                sock.Receive(sizeBuf);

                byte[] okBuf = Encoding.ASCII.GetBytes(Convert.ToString("\n"));
                sock.Send(okBuf);

                UInt64 oSize = Convert.ToUInt64(Encoding.ASCII.GetString(sizeBuf));
                UInt64 size = 0;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    InformationTip.IsOpen = false;
                    DownloadProgress.Maximum = oSize;
                    DownloadProgress.Visibility = Visibility.Visible;
                    ProgressText.Text = "Downloading \"" + filename + "\"";
                });
                StorageFile file = null;
                try
                {
                    file = await Windows.Storage.DownloadsFolder.CreateFileAsync(filename);
                }
                catch (Exception)
                {
                    InfoPopup("File Error", "File Already Exists");
                    WriteLog("[Filesystem] - File Already Exists\n");
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DownloadProgress.Visibility = Visibility.Collapsed;
                        ProgressText.Text = "";
                        socketTrackers[0].sock.Close();
                        socketTrackers.RemoveAt(0);
                    });
                    ReadyForQueue = true;
                    return;
                }
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                using (var outputStream = stream.GetOutputStreamAt(stream.Position))
                {
                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                    {
                        while (size < oSize)
                        {
                            byte[] fileBuffer = new byte[10240];
                            int Rsize = sock.Receive(fileBuffer);
                            for(int i = 0; i < Rsize; i++)
                            {
                                dataWriter.WriteByte(fileBuffer[i]);
                            }
                            size += (ulong)Rsize;
                            await dataWriter.StoreAsync();
                            await outputStream.FlushAsync();
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                DownloadProgress.Value = size;
                            });
                        }
                    }
                }
                stream.Dispose();
                socketTrackers.RemoveAt(0);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    ProgressText.Text = "";
                });

                InfoPopup("Download Complete", filename + " was downloaded");
                ReadyForQueue = true;
            }
            catch (Exception error)
            {
                InfoPopup("Socket Error", "Download canceled");
                WriteLog("[Socket Failure] - " + error.Message + "\n");
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    ProgressText.Text = "";
                });
                socketTrackers[0].sock.Close();
                ReadyForQueue = true;
                socketTrackers.RemoveAt(0);
                return;
            }
        }

        // Client Functions
        public void ReceiveAsync(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                //RecvAllDone.Set();

                // Get the socket that handles the client request.  
                Connection ListItem = (Connection)ar.AsyncState;
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].sock.EndReceive(ar);

                // Get the request

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Set();
            }
            catch (Exception)
            {
                return;
            }

        }
        public void ConnectAsync(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                //RecvAllDone.Set();

                // Get the socket that handles the client request.  
                Connection ListItem = (Connection)ar.AsyncState;
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].sock.EndConnect(ar);

                // Get the request

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Set();
            }
            catch (Exception)
            {
                return;
            }

        }
        public async void FileSendRequestHandler(object data)
        {
            Windows.Storage.StorageFile file = (Windows.Storage.StorageFile)data;
            Contact selectedItem = new Contact();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                selectedItem = (Contact)ContactsListView.SelectedItem; // Get contact object
            });
            Windows.Storage.FileProperties.BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            string fileSize = string.Format("{0:n0}", basicProperties.Size);
            int id = 0;
            // Connect to the endpoint
            IPAddress ipAddr = IPAddress.Parse(selectedItem.IP);
            IPEndPoint EndPoint = new IPEndPoint(ipAddr, 47);
            Socket sock = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Connection ListItem = null;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ListItem = connectionsViewModel.AddConnection(file.Name, selectedItem.Name, sock);
            });

            try
            {
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Reset();
                sock.BeginConnect(EndPoint, new AsyncCallback(ConnectAsync), ListItem);
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(ConnectionTimeout);
                if (!sock.Connected)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                        connectionsViewModel.RemoveConnection(ListItem);
                    });
                    return;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].progressBar = true;
                });

                string rq =
                    "HRZNRQ/" +
                    PublicIP +
                    "/" +
                    file.Name +
                    "/" +
                    fileSize;


                byte[] messageSent = Encoding.ASCII.GetBytes(rq);
                int byteSent = sock.Send(messageSent);

                InfoPopup("Request Sent", "Waiting on Response");

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Reset();
                byte[] buffer = new byte[10];
                sock.BeginReceive(buffer, 0, 10, 0, new AsyncCallback(ReceiveAsync), ListItem);

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(AcceptTimeout);

                string request = new string(Encoding.ASCII.GetChars(buffer));
                if(request == "HRZNACCEPT")
                {
                    SendFile(sock, file);
                }
                else if (request == "HRZNDENY")
                {
                    InfoPopup("Send Canceled", "Client denied the request");
                }
                else
                {
                    sock.Close();
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                    connectionsViewModel.RemoveConnection(ListItem);
                });
            }
            catch (Exception error)
            {
                InfoPopup("Socket Error", error.Message);
                WriteLog("[Socket Failure] - " + error.Message + "\n");
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.RemoveConnection(ListItem);
                });
                return;
            }
        }
        public async void FileSendToRequestingHandler(object data)
        {
            Windows.Storage.StorageFile file = (Windows.Storage.StorageFile)data;
            Contact selectedItem = new Contact();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                selectedItem = (Contact)ContactsListView.SelectedItem; // Get contact object
            });
            Windows.Storage.FileProperties.BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            string fileSize = string.Format("{0:n0}", basicProperties.Size);
            int id = 0;
            Socket sock = socketTrackers[0].sock;
            Connection ListItem = null;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ListItem = connectionsViewModel.AddConnection(file.Name, socketTrackers[0].args[2], sock);
            });

            try
            {
                if (!sock.Connected)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                        connectionsViewModel.RemoveConnection(ListItem);
                    });
                    return;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].progressBar = true;
                });

                string rq =
                    "HRZNACCEPT/" +
                    PublicIP +
                    "/" +
                    file.Name +
                    "/" +
                    fileSize;


                byte[] messageSent = Encoding.ASCII.GetBytes(rq);
                int byteSent = sock.Send(messageSent);

                InfoPopup("Request Sent", "Waiting on Response");

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Reset();
                byte[] buffer = new byte[10];
                sock.BeginReceive(buffer, 0, 10, 0, new AsyncCallback(ReceiveAsync), ListItem);

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(AcceptTimeout);

                string request = new string(Encoding.ASCII.GetChars(buffer));
                if (request == "HRZNACCEPT")
                {
                    SendFile(sock, file);
                }
                else if (request == "HRZNDENY")
                {
                    InfoPopup("Send Canceled", "Client denied the request");
                }
                else
                {
                    sock.Close();
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                    connectionsViewModel.RemoveConnection(ListItem);
                });
            }
            catch (Exception error)
            {
                InfoPopup("Socket Error", error.Message);
                WriteLog("[Socket Failure] - " + error.Message + "\n");
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.RemoveConnection(ListItem);
                });
                return;
            }
        }
        public async void FileReceiveRequestHandler()
        {
            Contact selectedItem = new Contact();
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                selectedItem = (Contact)ContactsListView.SelectedItem; // Get contact object
            });
            // Connect to the endpoint
            IPAddress ipAddr = IPAddress.Parse(selectedItem.IP);
            IPEndPoint EndPoint = new IPEndPoint(ipAddr, 47);
            Socket sock = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Connection ListItem = null;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ListItem = connectionsViewModel.AddConnection("[Requesting]", selectedItem.Name, sock);
            });

            try
            {
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Reset();
                sock.BeginConnect(EndPoint, new AsyncCallback(ConnectAsync), ListItem);
                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(ConnectionTimeout);
                if (!sock.Connected)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                        connectionsViewModel.RemoveConnection(ListItem);
                    });
                    return;
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].progressBar = true;
                });

                string rq =
                    "HRZNRQ/Recv/" +
                    PublicIP;


                byte[] messageSent = Encoding.ASCII.GetBytes(rq);
                int byteSent = sock.Send(messageSent);

                InfoPopup("Request Sent", "Waiting on Response");

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.Reset();
                byte[] buffer = new byte[1024];
                sock.BeginReceive(buffer, 0, 1024, 0, new AsyncCallback(ReceiveAsync), ListItem);

                connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(AcceptTimeout);

                string request = new string(Encoding.ASCII.GetChars(buffer));
                if (request.Contains("HRZNACCEPT"))
                {
                    string[] split = request.Split("/");
                    if (split.Length == 4)
                    {
                        socketTrackers.Add(new SocketTracker(split, sock, "RECV"));
                        WaitForSendFileAccept(split, sock);
                    }
                }
                else if (request == "HRZNDENY")
                {
                    InfoPopup("Request Denied", "Server denied the request");
                }
                else
                {
                    sock.Close();
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.Connections[connectionsViewModel.GetIndex(ListItem)].handler.WaitOne(100);
                    connectionsViewModel.RemoveConnection(ListItem);
                });
            }
            catch (Exception error)
            {
                InfoPopup("Socket Error", error.Message);
                WriteLog("[Socket Failure] - " + error.Message + "\n");
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    connectionsViewModel.RemoveConnection(ListItem);
                });
                return;
            }
        }
        public async void SendFile(Socket fs, Windows.Storage.StorageFile file)
        {
            BasicProperties filesize = await file.GetBasicPropertiesAsync();
            var size = filesize.Size;
            byte[] oSizeStr = Encoding.ASCII.GetBytes(Convert.ToString(filesize.Size));
            fs.Send(oSizeStr);
            byte[] okBuf = new byte[1];
            fs.Receive(okBuf);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                InformationTip.IsOpen = false;
                DownloadProgress.Maximum = size;
                DownloadProgress.Visibility = Visibility.Visible;
                ProgressText.Text = "Uploading \"" + file.Name + "\"";
            });
            ulong sizeSent = 0;
            byte[] buffer = new byte[10240];
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            ulong streamSize = stream.Size;
            var inputStream = stream.GetInputStreamAt(0);
            var dataReader = new Windows.Storage.Streams.DataReader(inputStream);
            try
            {
                while (sizeSent < size)
                {
                    if (size - sizeSent > 10240)
                    {
                        await dataReader.LoadAsync((uint)10240);
                        dataReader.ReadBytes(buffer);
                        sizeSent += (ulong)fs.Send(buffer);
                    }
                    else
                    {
                        await dataReader.LoadAsync((uint)(size - sizeSent));
                        var buf = dataReader.ReadBuffer((uint)(size - sizeSent));
                        CryptographicBuffer.CopyToByteArray(buf, out buffer);
                        sizeSent += (ulong)fs.Send(buffer);
                    }
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DownloadProgress.Value = sizeSent;
                    });
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    ProgressText.Text = "";
                });
                InfoPopup("Upload Complete", file.Name + " was sent");
            }
            catch (Exception error)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    ProgressText.Text = "";
                });
                InfoPopup("Upload Failed", error.Message);
                WriteLog("[Upload Failure] - " + error.Message + "\n");
                return;
            }
        }

        // Handler Functions
        private void AddContactDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            NameInput.Text = "";
            IPInput.Text = "";
            if(openingChecks.IPTip == true){
                IPButtonTip.IsOpen = true;
            }
        }
        private void IPButtonTip_Closing(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosingEventArgs args)
        {
            openingChecks.IPTip = false;
            openingChecks.ContactsTip = false;
        }
        private void AddContactButtonTip_Closing(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosingEventArgs args)
        {
            openingChecks.IPTip = true;
        }

    }
}
