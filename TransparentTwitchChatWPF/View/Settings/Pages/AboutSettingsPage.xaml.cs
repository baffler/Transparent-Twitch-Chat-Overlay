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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings
{
    /// <summary>
    /// Interaction logic for AboutSettingsPage.xaml
    /// </summary>
    public partial class AboutSettingsPage : UserControl
    {
        public AboutSettingsPage()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
