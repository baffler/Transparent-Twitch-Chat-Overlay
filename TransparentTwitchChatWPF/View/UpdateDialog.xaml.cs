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
using Velopack;

namespace TransparentTwitchChatWPF.View
{
    /// <summary>
    /// Interaction logic for UpdateDialog.xaml
    /// </summary>
    public partial class UpdateDialog : Window
    {
        // Public property to access the checkbox state from outside
        public bool ShouldDisableUpdates => DisableUpdateCheckBox.IsChecked == true;

        public UpdateDialog(string currentVersion, string newVersion, VelopackAsset release = null)
        {
            InitializeComponent();
            CurrentVersionText.Text = currentVersion;
            NewVersionText.Text = newVersion;
            if (release != null)
            {
                //ReleaseNotesText.Text = release.NotesHTML;
            }
        }
        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
