using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WebRtcClient.Utilities;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebRtcClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public string ServerAddress { get; set; } = "10.0.0.192";
        public bool Logging { get; set; } = true;

        private Client Client { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }

            this.Client = new Client(this.Dispatcher, this.RemoteVideo, this.ServerAddress);
        }

        #region Utilities

        private async void WriteLine(string Message)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.OutputTextBox.Text += Message + Environment.NewLine;
            });
        }

        #endregion


        #region UI_Handlers

        private async void ClientConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.SettingsPanel.Visibility = Visibility.Collapsed;
            try
            {
                await this.Client.Initialize();
                await this.Client.ConnectToServer();
                this.OutputTextBox.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Log($"{ex}");
            }
        }

        private void LoggerToggle_Click(object sender, RoutedEventArgs e)
        {
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }
            else
            {
                Logger.WriteMessage -= this.WriteLine;
            }
        }

        #endregion
    }
}
