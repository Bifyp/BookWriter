using System.Windows;
using System.Windows.Input;

namespace BookWriter.Views
{
    public partial class InputDialog : Window
    {
        public string Result { get; private set; } = string.Empty;

        public InputDialog(string title, string label, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            LabelText.Text = label;
            InputBox.Text = defaultValue;
            Loaded += (_, _) => { InputBox.Focus(); InputBox.SelectAll(); };
        }

        private void OK_Click(object s, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

        private void InputBox_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Result = InputBox.Text; DialogResult = true; }
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
