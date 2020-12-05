﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Horizon
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Help : Page
    {
        string AccentColor;
        public Help()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            AccentColor = (string)localSettings.Values["Accent"];
            this.InitializeComponent();
            if(openingChecks.ServerClosedTip == true)
            {
                ServerClosedTip.IsOpen = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            openingChecks.ServerClosedTip = false;
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void PortForwardingLink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.noip.com/support/knowledgebase/general-port-forwarding-guide/"));
        }

        private void AddContactLink_Click(object sender, RoutedEventArgs e)
        {
            openingChecks.ContactsTip = true;
            openingChecks.ServerClosedTip = false;
            this.Frame.Navigate(typeof(MainPage));
        }

        private void ServerToggleLink_Click(object sender, RoutedEventArgs e)
        {
            openingChecks.ServerToggleTip = true;
            openingChecks.ServerClosedTip = false;
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void KofiButton_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }

        private async void emaillink_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:epsilon@epsirho.com"));
        }
    }
}