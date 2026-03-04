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

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for Input.xaml
    /// </summary>
    public partial class Input_Custom : Window
    {
        public Input_Custom(string Url = "")
        {
            InitializeComponent();
            tbCustomCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
            txtUrl.Text = Url;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtUrl.SelectAll();
            txtUrl.Focus();
        }

        public string Url
        {
            get { return txtUrl.Text; }
        }

        public string DisplayName
        {
            get { return txtName.Text; }
        }

        public string CustomCSS
        {
            get { return tbCustomCSS.Text; }
        }

        public bool AllowInteraction
        {
            get { return cbAllowInteraction.IsChecked ?? false; }
        }
    }
}
