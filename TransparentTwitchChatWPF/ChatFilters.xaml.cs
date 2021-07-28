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
using System.Collections.Specialized;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for ChatFilters.xaml
    /// </summary>
    public partial class ChatFilters : Window
    {
        StringCollection sc = new StringCollection();

        public ChatFilters()
        {
            InitializeComponent();
            foreach (string s in SettingsSingleton.Instance.genSettings.AllowedUsersList)
                sc.Add(s);

            refreshLB();

            this.cbHighlightUsers.IsChecked = SettingsSingleton.Instance.genSettings.HighlightUsersChat;
            this.cbAllowedUsers.IsChecked = SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat;
            this.cbAllMods.IsChecked = SettingsSingleton.Instance.genSettings.FilterAllowAllMods;
            this.cbAllVIPs.IsChecked = SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs;
            this.cbShowBotActivity.IsChecked = SettingsSingleton.Instance.genSettings.ShowBotActivity;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.lvAllowedUsernames.SelectedIndex >= 0)
            {
                this.sc.Remove(this.lvAllowedUsernames.SelectedItem as string);
                refreshLB();
            }
            //this.lvWhitelistUsernames.Items.Remove(this.lvWhitelistUsernames.SelectedItem);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Input_Username inputDialog = new Input_Username();
            if (inputDialog.ShowDialog() == true)
            {
                this.sc.Add(inputDialog.Username);
                refreshLB();
            }
        }

        private void refreshLB()
        {
            this.lvAllowedUsernames.Focus();
            this.lvAllowedUsernames.UnselectAll();
            this.lvAllowedUsernames.Items.Clear();

            foreach (string s in this.sc)
                this.lvAllowedUsernames.Items.Add(s);
            
            this.lvAllowedUsernames.UnselectAll();
            this.lvAllowedUsernames.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsSingleton.Instance.genSettings.HighlightUsersChat = this.cbHighlightUsers.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat = this.cbAllowedUsers.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.FilterAllowAllMods = this.cbAllMods.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs = this.cbAllVIPs.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.AllowedUsersList = sc;
            SettingsSingleton.Instance.genSettings.ShowBotActivity = this.cbShowBotActivity.IsChecked ?? false;
            DialogResult = true;
        }

        private void cbAllowedUsers_Checked(object sender, RoutedEventArgs e)
        {
            cbHighlightUsers.IsChecked = false;
            cbAllMods.IsEnabled = true;
            cbAllMods.Content = "Allow all mods";
            cbAllVIPs.IsEnabled = true;
            cbAllVIPs.Content = "Allow all VIPs";
            btnAddUser.IsEnabled = true;
            btnRemoveUser.IsEnabled = true;
            lvAllowedUsernames.IsEnabled = true;
        }

        private void cbAllowedUsers_Unchecked(object sender, RoutedEventArgs e)
        {
            cbAllMods.IsEnabled = false;
            cbAllVIPs.IsEnabled = false;
            btnAddUser.IsEnabled = false;
            btnRemoveUser.IsEnabled = false;
            lvAllowedUsernames.IsEnabled = false;
        }

        private void cbHighlightUsers_Checked(object sender, RoutedEventArgs e)
        {
            cbAllowedUsers.IsChecked = false;
            cbAllMods.IsEnabled = true;
            cbAllMods.Content = "Highlight all mods";
            cbAllVIPs.IsEnabled = true;
            cbAllVIPs.Content = "Highlight all VIPs";
            btnAddUser.IsEnabled = true;
            btnRemoveUser.IsEnabled = true;
            lvAllowedUsernames.IsEnabled = true;
        }

        private void cbHighlightUsers_Unchecked(object sender, RoutedEventArgs e)
        {
            cbAllMods.IsEnabled = false;
            cbAllVIPs.IsEnabled = false;
            btnAddUser.IsEnabled = false;
            btnRemoveUser.IsEnabled = false;
            lvAllowedUsernames.IsEnabled = false;
        }
    }
}
