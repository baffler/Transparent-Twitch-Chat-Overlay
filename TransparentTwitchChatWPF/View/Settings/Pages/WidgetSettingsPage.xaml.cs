using System.Windows;
using System.Windows.Controls;
using TransparentTwitchChatWPF.Utils;

namespace TransparentTwitchChatWPF.View.Settings;

/// <summary>
/// Interaction logic for WidgesSettingsPage.xaml
/// </summary>
public partial class WidgetSettingsPage : UserControl
{
    public event Action<string, string> WidgetCreationRequested;

    public WidgetSettingsPage()
    {
        InitializeComponent();

        tbWidgetCustomCSS.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
    }

    private void NewWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        WidgetCreationRequested?.Invoke(this.tbUrlForWidget.Text, this.tbWidgetCustomCSS.Text);
        this.tbUrlForWidget.Text = string.Empty;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        ShellHelper.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
