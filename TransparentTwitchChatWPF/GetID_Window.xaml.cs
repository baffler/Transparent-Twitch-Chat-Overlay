using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Navigation;
using TwitchLib.Api;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for GetID_Window.xaml
    /// </summary>
    public partial class GetID_Window : Window
    {
        private TwitchAPI _api;
        private string _username = "";

        public GetID_Window(string Username)
        {
            this._username = Username;
            InitializeComponent();
            this.Title = $"Get Channel ID for '{Username}'";
            this.lblChannelID.Content = $"Channel ID (for '{Username}'):";
            
            if (string.IsNullOrEmpty(Username))
            {
                tbChannelID.Text = "(Username field is empty)";
                btnFetchID.IsEnabled = false;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void btnFetchID_Click(object sender, RoutedEventArgs e)
        {
            if ((string)btnFetchID.Content == "OK")
            {
                DialogResult = true;
            }
            else
            {
                tbChannelID.Text = "(ID will be populated here)";
                btnFetchID.IsEnabled = false;
                btnFetchID.Content = "Wait...";

                
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
