using System.Windows;
using System.Windows.Controls;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for WidgesSettingsPage.xaml
/// </summary>
public partial class WidgetSettingsPage : UserControl
{
    public WidgetSettingsPage()
    {
        InitializeComponent();

        tbWidgetCustomCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
    }

    private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: create a new widget window with the provided URL and custom CSS.
        //this._main.CreateNewWindow(this.tbUrlForWidget.Text, this.tbWidgetCustomCSS.Text);
        this.tbUrlForWidget.Text = string.Empty;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
