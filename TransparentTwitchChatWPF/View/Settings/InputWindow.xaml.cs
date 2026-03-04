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

namespace TransparentTwitchChatWPF.View.Settings
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        // Public property to hold the user's response
        public string ResponseText { get; private set; }

        public InputWindow(string prompt, string defaultText = "")
        {
            InitializeComponent();
            PromptText.Text = prompt;
            ResponseTextBox.Text = defaultText;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = ResponseTextBox.Text;
            this.DialogResult = true; // Signals that the user clicked OK
        }
    }
}
