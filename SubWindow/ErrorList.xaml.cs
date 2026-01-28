using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MajdataEdit
{
    public enum ErrorType
    {
        Other,
        Info,
        MuriDXS,
        MuriDXD,
        Syntax,
        Serialize
    }
    public class Position
    {
        public int x; //column
        public int y; //Line
        public Position(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public override string ToString()
        {
            return $"L{y},C{x}";
        }
    }
    public class Error
    {
        public ErrorType Type { get; set; }
        public Position Position { get; set; }
        public string Message { get; set; }
        public string? Detail { get; set; }
        public Error(ErrorType _type, Position _position, string _message, string? _detail)
        {
            Type = _type;
            Position = _position;
            Message = _message;
            Detail = _detail;
        }
    }

    /// <summary>
    /// ErrorList.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorList : Window
    {
        public ErrorList()
        {
            InitializeComponent();
        }

        private void ErrorListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtDetail.Text = (ErrorListView.SelectedItem as Error)?.Detail ?? "";
        }

        private void ErrorListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Error error = (ErrorListView.SelectedItem as Error)!;
            if (error == null) return;
            var owner = (MainWindow)Owner;
            owner.FumenContent.ScrollToVerticalOffset((error.Position.y - 1) * 28);
            owner.SetRawFumenPosition(error.Position.x, error.Position.y - 1);
            owner.FumenContent.Focus();
            owner.Activate();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            this.Hide();
            Owner.Activate();
            e.Cancel = true;
        }
    }
}
