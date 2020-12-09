using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Horizon
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        string AccentColor;
        string UnkownIpAllow;
        int ConnectionTimeout;
        int AcceptTimeout;
        public Settings()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            AccentColor = (string)localSettings.Values["Accent"];
            UnkownIpAllow = (string)localSettings.Values["UnkownIpAllow"];
            ConnectionTimeout = (Int32)localSettings.Values["ConnectionTimeout"];
            AcceptTimeout = (Int32)localSettings.Values["AcceptTimeout"];
            this.InitializeComponent();
        }
        private void AccentButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccentSwitcher.IsOpen)
            {
                AccentSwitcher.IsOpen = false;
            }
            else
            {
                AccentSwitcher.IsOpen = true;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            AccentColor = (string)localSettings.Values["Accent"];
            string colorName = e.AddedItems[0].ToString();
            switch (colorName)
            {
                case "Default Blue":
                    localSettings.Values["Accent"] = "#FF1EAED8";
                    break;
                case "Green":
                    localSettings.Values["Accent"] = "#FF2CBB32";
                    break;
                case "Red":
                    localSettings.Values["Accent"] = "#FFFF3030";
                    break;
                case "Yellow":
                    localSettings.Values["Accent"] = "#FFE8FF00";
                    break;
                case "Purple":
                    localSettings.Values["Accent"] = "#FF71039B";
                    break;
                case "Pink":
                    localSettings.Values["Accent"] = "#FFFF00A2";
                    break;
            }
            this.Frame.Navigate(typeof(Settings));
            AccentSwitcher.IsOpen = false;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void UnknownIpButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            UnkownIpAllow = (string)localSettings.Values["UnkownIpAllow"];
            if(UnkownIpAllow == "false")
            {
                localSettings.Values["UnkownIpAllow"] = "true";
            }
            else
            {
                localSettings.Values["UnkownIpAllow"] = "false";
            }
            this.Frame.Navigate(typeof(Settings));
        }

        private void ConnectionTimeoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionTimeoutPopup.IsOpen)
            {
                ConnectionTimeoutPopup.IsOpen = false;
            }
            else
            {
                ConnectionTimeoutPopup.IsOpen = true;
                AcceptTimeoutPopup.IsOpen = false;
                TimeoutError.IsOpen = false;
            }
        }

        private void AcceptTimeoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (AcceptTimeoutPopup.IsOpen)
            {
                AcceptTimeoutPopup.IsOpen = false;
            }
            else
            {
                AcceptTimeoutPopup.IsOpen = true;
                ConnectionTimeoutPopup.IsOpen = false;
                TimeoutError.IsOpen = false;
            }
        }

        private void AcceptPopupButton_Click(object sender, RoutedEventArgs e)
        {
            if(AcceptTimeoutTextBox.Text == "")
            {
                AcceptTimeoutPopup.IsOpen = false;
                TimeoutError.Target = AcceptTimeoutButton;
                TimeoutError.IsOpen = true;
                return;
            }
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["AcceptTimeout"] = Int32.Parse(AcceptTimeoutTextBox.Text);
                AcceptTimeoutPopup.IsOpen = false;
                this.Frame.Navigate(typeof(Settings));
            }
            catch (Exception)
            {
                AcceptTimeoutPopup.IsOpen = false;
                TimeoutError.Target = AcceptTimeoutButton;
                TimeoutError.IsOpen = true;
                AcceptTimeoutTextBox.Text = "";
            }
        }

        private void ConnectionPopupButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionTimeoutTextBox.Text == "")
            {
                ConnectionTimeoutPopup.IsOpen = false;
                TimeoutError.Target = ConnectionTimeoutButton;
                TimeoutError.IsOpen = true;
                return;
            }
            try
            {
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["ConnectionTimeout"] = Int32.Parse(ConnectionTimeoutTextBox.Text);
                ConnectionTimeoutPopup.IsOpen = false;
                this.Frame.Navigate(typeof(Settings));
            }
            catch (Exception)
            {
                ConnectionTimeoutPopup.IsOpen = false;
                TimeoutError.Target = ConnectionTimeoutButton;
                TimeoutError.IsOpen = true;
                ConnectionTimeoutTextBox.Text = "";
            }
        }
    }
}
