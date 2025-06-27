using System.Windows;
using System.Windows.Input;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for HotkeyEditorControl.xaml
    /// </summary>
    public partial class HotkeyEditorControl
    {
        public static readonly DependencyProperty HotkeyProperty =
            DependencyProperty.Register(
                nameof(Hotkey),
                typeof(Hotkey),
                typeof(HotkeyEditorControl),
                new FrameworkPropertyMetadata(
                    default(Hotkey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                )
            );

        public bool IsCapturing { get; private set; } = false;

        public Hotkey Hotkey
        {
            get => (Hotkey)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        public HotkeyEditorControl()
        {
            InitializeComponent();
            HotkeyTextBox.Focusable = false;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Don't let the event pass further because we don't want
            // standard textbox shortcuts to work.
            e.Handled = true;

            if (!IsCapturing)
                return;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                Hotkey = null;
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            // Update the value
            Hotkey = new Hotkey(key, modifiers);
        }

        public void StartCapturing()
        {
            IsCapturing = true;
            //this.IsEnabled = true;
            HotkeyTextBox.Focusable = true;
            HotkeyTextBox.Focus();
        }

        public void StopCapturing()
        {
            HotkeyTextBox.Focusable = false;
            IsCapturing = false;
            //this.IsEnabled = false;
        }
    }
}
