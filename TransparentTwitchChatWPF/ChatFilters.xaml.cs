using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Diagnostics;

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

            if (App.Settings.GeneralSettings.AllowedUsersList == null)
                App.Settings.GeneralSettings.AllowedUsersList = new StringCollection();
            if (App.Settings.GeneralSettings.BlockedUsersList == null)
                App.Settings.GeneralSettings.BlockedUsersList = new StringCollection();

            foreach (string s in App.Settings.GeneralSettings.AllowedUsersList)
                scAllowedUsers.Add(s);

            foreach (string s in App.Settings.GeneralSettings.BlockedUsersList)
                scBlockedUsers.Add(s);

            refreshListBoxAllowedUsers();
            refreshListBoxBlockedUsers();

            this.cbHighlightUsers.IsChecked = App.Settings.GeneralSettings.HighlightUsersChat;
            this.cbAllowedUsers.IsChecked = App.Settings.GeneralSettings.AllowedUsersOnlyChat;
            this.cbAllMods.IsChecked = App.Settings.GeneralSettings.FilterAllowAllMods;
            this.cbAllVIPs.IsChecked = App.Settings.GeneralSettings.FilterAllowAllVIPs;
            this.cbBlockBotActivity.IsChecked = App.Settings.GeneralSettings.BlockBotActivity;
            this.colorPicker.SelectedColor = App.Settings.GeneralSettings.ChatHighlightColor;
            this.colorPickerMods.SelectedColor = App.Settings.GeneralSettings.ChatHighlightModsColor;
            this.colorPickerVIPs.SelectedColor = App.Settings.GeneralSettings.ChatHighlightVIPsColor;
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
            App.Settings.GeneralSettings.HighlightUsersChat = this.cbHighlightUsers.IsChecked ?? false;
            App.Settings.GeneralSettings.AllowedUsersOnlyChat = this.cbAllowedUsers.IsChecked ?? false;
            App.Settings.GeneralSettings.FilterAllowAllMods = this.cbAllMods.IsChecked ?? false;
            App.Settings.GeneralSettings.FilterAllowAllVIPs = this.cbAllVIPs.IsChecked ?? false;
            App.Settings.GeneralSettings.AllowedUsersList = scAllowedUsers;
            App.Settings.GeneralSettings.BlockedUsersList = scBlockedUsers;
            App.Settings.GeneralSettings.BlockBotActivity = this.cbBlockBotActivity.IsChecked ?? false;
            App.Settings.GeneralSettings.ChatHighlightColor = this.colorPicker.SelectedColor ?? App.Settings.GeneralSettings.ChatHighlightColor;
            App.Settings.GeneralSettings.ChatHighlightModsColor = this.colorPickerMods.SelectedColor ?? App.Settings.GeneralSettings.ChatHighlightModsColor;
            App.Settings.GeneralSettings.ChatHighlightVIPsColor = this.colorPickerVIPs.SelectedColor ?? App.Settings.GeneralSettings.ChatHighlightVIPsColor;
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
