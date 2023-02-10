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
        StringCollection scAllowedUsers = new StringCollection();
        StringCollection scBlockedUsers = new StringCollection();

        public ChatFilters()
        {
            InitializeComponent();

            if (SettingsSingleton.Instance.genSettings.AllowedUsersList == null)
                SettingsSingleton.Instance.genSettings.AllowedUsersList = new StringCollection();
            if (SettingsSingleton.Instance.genSettings.BlockedUsersList == null)
                SettingsSingleton.Instance.genSettings.BlockedUsersList = new StringCollection();

            foreach (string s in SettingsSingleton.Instance.genSettings.AllowedUsersList)
                scAllowedUsers.Add(s);

            foreach (string s in SettingsSingleton.Instance.genSettings.BlockedUsersList)
                scBlockedUsers.Add(s);

            refreshListBoxAllowedUsers();
            refreshListBoxBlockedUsers();

            this.cbHighlightUsers.IsChecked = SettingsSingleton.Instance.genSettings.HighlightUsersChat;
            this.cbAllowedUsers.IsChecked = SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat;
            this.cbAllMods.IsChecked = SettingsSingleton.Instance.genSettings.FilterAllowAllMods;
            this.cbAllVIPs.IsChecked = SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs;
            this.cbBlockBotActivity.IsChecked = SettingsSingleton.Instance.genSettings.BlockBotActivity;
        }

        private void OnClick_RemoveAllowedUsername(object sender, RoutedEventArgs e)
        {
            if (this.lvAllowedUsernames.SelectedIndex >= 0)
            {
                this.scAllowedUsers.Remove(this.lvAllowedUsernames.SelectedItem as string);
                refreshListBoxAllowedUsers();
            }
            //this.lvWhitelistUsernames.Items.Remove(this.lvWhitelistUsernames.SelectedItem);
        }

        private void OnClick_AddAllowedUsername(object sender, RoutedEventArgs e)
        {
            Input_Username inputDialog = new Input_Username();
            if (inputDialog.ShowDialog() == true)
            {
                this.scAllowedUsers.Add(inputDialog.Username);
                refreshListBoxAllowedUsers();
            }
        }

        private void OnClick_RemoveBlockedUsername(object sender, RoutedEventArgs e)
        {
            if (this.lvBlockedUsernames.SelectedIndex >= 0)
            {
                this.scBlockedUsers.Remove(this.lvBlockedUsernames.SelectedItem as string);
                refreshListBoxBlockedUsers();
            }
        }

        private void OnClick_AddBlockedUsername(object sender, RoutedEventArgs e)
        {
            Input_Username inputDialog = new Input_Username();
            if (inputDialog.ShowDialog() == true)
            {
                this.scBlockedUsers.Add(inputDialog.Username);
                refreshListBoxBlockedUsers();
            }
        }

        private void refreshListBoxAllowedUsers()
        {
            this.lvAllowedUsernames.Focus();
            this.lvAllowedUsernames.UnselectAll();
            this.lvAllowedUsernames.Items.Clear();

            foreach (string s in this.scAllowedUsers)
                this.lvAllowedUsernames.Items.Add(s);
            
            this.lvAllowedUsernames.UnselectAll();
            this.lvAllowedUsernames.Focus();
        }

        private void refreshListBoxBlockedUsers()
        {
            this.lvBlockedUsernames.Focus();
            this.lvBlockedUsernames.UnselectAll();
            this.lvBlockedUsernames.Items.Clear();

            foreach (string s in this.scBlockedUsers)
                this.lvBlockedUsernames.Items.Add(s);

            this.lvBlockedUsernames.UnselectAll();
            this.lvBlockedUsernames.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsSingleton.Instance.genSettings.HighlightUsersChat = this.cbHighlightUsers.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.AllowedUsersOnlyChat = this.cbAllowedUsers.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.FilterAllowAllMods = this.cbAllMods.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.FilterAllowAllVIPs = this.cbAllVIPs.IsChecked ?? false;
            SettingsSingleton.Instance.genSettings.AllowedUsersList = scAllowedUsers;
            SettingsSingleton.Instance.genSettings.BlockedUsersList = scBlockedUsers;
            SettingsSingleton.Instance.genSettings.BlockBotActivity = this.cbBlockBotActivity.IsChecked ?? false;
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

        private void lvFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.lvFilters.SelectedIndex)
            {
                case 0: // Allowed usernames/ Highlighting
                    filterUsernamesGrid.Visibility = Visibility.Visible;
                    filterBotsGrid.Visibility = Visibility.Hidden;
                    break;
                case 1: // Blocked usernames/ Bots
                    filterUsernamesGrid.Visibility = Visibility.Hidden;
                    filterBotsGrid.Visibility = Visibility.Visible;
                    break;
                case 2: // Blocked words
                    break;

            }
        }
    }
}
