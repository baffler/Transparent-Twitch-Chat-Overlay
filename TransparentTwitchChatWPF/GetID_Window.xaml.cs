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
using TwitchLib;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.V5.Models.Users;

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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
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

                _api = new TwitchAPI();
                _api.Settings.AccessToken = tbAccessToken.Text;
                _api.Settings.ClientId = tbClientID.Text;

                _getChannelID(this._username);
            }
        }

        private async void _getChannelID(string channel)
        {
            //GetUsersResponse getUsersResponse = null;
            Users users = null;

            try
            {
                users = await _api.V5.Users.GetUserByNameAsync(channel);
                ///getUsersResponse = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { channel });
            } 
            catch (Exception e) 
            {
                tbChannelID.Text = "(Error fetching channel ID)!!";
                btnFetchID.IsEnabled = true;
                btnFetchID.Content = "Fetch ID";
                MessageBox.Show(e.Message); 
            }

            /*if (getUsersResponse != null)
            {
                var users = getUsersResponse.Users;
                if (users.Length > 0)
                {
                    SettingsSingleton.Instance.genSettings.ChannelID = users[0].Id;
                    tbChannelID.Text = users[0].Id;
                    btnFetchID.Content = "OK";
                    btnFetchID.IsEnabled = true;
                }
                else
                {
                    tbChannelID.Text = "(Error fetching channel ID)";
                    btnFetchID.IsEnabled = true;
                    btnFetchID.Content = "Fetch ID";
                }
            }*/

            if (users != null)
            {
                if (users.Total > 0)
                {
                    SettingsSingleton.Instance.genSettings.ChannelID = users.Matches[0].Id;
                    tbChannelID.Text = users.Matches[0].Id;
                    btnFetchID.Content = "OK";
                    btnFetchID.IsEnabled = true;
                }
                else
                {
                    tbChannelID.Text = "(Error fetching channel ID)";
                    btnFetchID.IsEnabled = true;
                    btnFetchID.Content = "Fetch ID";
                }
            }
            else
            {
                tbChannelID.Text = "(Error fetching channel ID)!";
                btnFetchID.IsEnabled = true;
                btnFetchID.Content = "Fetch ID";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
