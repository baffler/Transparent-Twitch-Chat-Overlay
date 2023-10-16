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
    public enum TextEditorType
    {
        Default, CSS, JavaScript
    }

    public class TextEditedEventArgs : EventArgs
    {
        public string EditedText { get; set; }
    }

    /// <summary>
    /// Interaction logic for TextEditorWindow.xaml
    /// </summary>
    public partial class TextEditorWindow : Window
    {
        public event EventHandler<TextEditedEventArgs> TextEdited;

        public TextEditorWindow(TextEditorType textEditorType, string initialText, string windowTitle = "")
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(windowTitle))
                this.Title = "Text Editor for custom " + textEditorType.ToString();
            else
                this.Title = windowTitle;

            this.textEditor.Text = initialText;

            if (textEditorType == TextEditorType.CSS)
                this.textEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("CSS");
            else if (textEditorType == TextEditorType.JavaScript)
                this.textEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("JavaScript");
            
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextEdited?.Invoke(this, new TextEditedEventArgs { EditedText = this.textEditor.Text });
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TextEdited?.Invoke(this, new TextEditedEventArgs { EditedText = this.textEditor.Text });
            this.Close();
        }

        private void CancelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
