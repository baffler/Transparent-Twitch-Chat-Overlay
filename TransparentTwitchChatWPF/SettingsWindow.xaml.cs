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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        WindowSettings config;

        public SettingsWindow(WindowSettings windowConfig)
        {
            this.config = windowConfig;

            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.config.isCustomURL = this.cbCustomURL.IsChecked ?? false;

            if (this.config.isCustomURL)
            {
                this.config.URL = this.tbURL.Text;

                if (!string.IsNullOrWhiteSpace(this.tbCSS2.Text) && !string.IsNullOrEmpty(this.tbCSS2.Text)
                    && (this.tbCSS2.Text.ToLower() != "css"))
                {
                    this.config.CustomCSS = this.tbCSS2.Text;
                }
                else
                    this.config.CustomCSS = string.Empty;
            }
            else
            {
                this.config.URL = string.Empty;
                this.config.Username = this.tbUsername.Text;
                this.config.ChatFade = this.cbFade.IsChecked ?? false;
                this.config.FadeTime = this.tbFadeTime.Text;
                this.config.ShowBotActivity = this.cbBotActivity.IsChecked ?? false;
                this.config.Theme = this.comboTheme.SelectedIndex;

                if (this.config.Theme == 0)
                {
                    this.config.CustomCSS = this.tbCSS.Text;
                }
            }

            DialogResult = true;
        }

        private void SetupValues()
        {
            this.tbUsername.Text = this.config.Username;
            this.cbFade.IsChecked = this.config.ChatFade;

            this.tbFadeTime.Text = this.config.FadeTime;
            this.tbFadeTime.IsEnabled = this.config.ChatFade;

            this.cbBotActivity.IsChecked = this.config.ShowBotActivity;
            this.comboTheme.SelectedIndex = this.config.Theme;

            if (this.config.isCustomURL)
            {
                this.sp1.Visibility = Visibility.Hidden;
                this.sp2.Visibility = Visibility.Hidden;
                this.tbURL.Visibility = Visibility.Visible;
                this.tbCSS2.Visibility = Visibility.Visible;

                this.tbURL.Text = this.config.URL;
                this.tbCSS2.Text = this.config.CustomCSS;

                this.cbCustomURL.IsChecked = true;
            }
            else
            {
                this.sp1.Visibility = Visibility.Visible;
                this.sp2.Visibility = Visibility.Visible;
                this.tbURL.Visibility = Visibility.Hidden;

                this.tbURL.Text = string.Empty;

                if (string.IsNullOrEmpty(this.config.CustomCSS))
                {
                    this.tbCSS.Text = @"::-webkit-scrollbar {
    visibility: hidden;
}

#chat_box {

}

.chat_line {

}

.chat_line .nick {

}

.chat_line .message {

}
";
                }
                else
                {
                    this.tbCSS.Text = this.config.CustomCSS;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void cbFade_Checked(object sender, RoutedEventArgs e)
        {
            this.tbFadeTime.IsEnabled = true;
        }

        private void cbFade_Unchecked(object sender, RoutedEventArgs e)
        {
            this.tbFadeTime.IsEnabled = false;
        }

        private void comboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboTheme.SelectedIndex == 0)
            {
                tbCSS.Visibility = Visibility.Visible;
                lblCSS.Visibility = Visibility.Visible;
                this.Height = 458;
            }
            else
            {
                tbCSS.Visibility = Visibility.Hidden;
                lblCSS.Visibility = Visibility.Hidden;
                this.Height = 355;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.sp1.Visibility = Visibility.Hidden;
            this.sp2.Visibility = Visibility.Hidden;
            this.Height = 300;

            this.tbURL.Visibility = Visibility.Visible;
            this.tbCSS2.Visibility = Visibility.Visible;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.tbURL.Visibility = Visibility.Hidden;
            this.tbCSS2.Visibility = Visibility.Hidden;

            this.sp1.Visibility = Visibility.Visible;
            this.sp2.Visibility = Visibility.Visible;

            if (comboTheme.SelectedIndex == 0)
                this.Height = 458;
            else
                this.Height = 355;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            SetupValues();
        }
    }
}
