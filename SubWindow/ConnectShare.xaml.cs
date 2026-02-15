using System.Windows;

namespace MajdataEdit
{
    /// <summary>
    /// ConnectShare.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectShare : Window
    {
        Action<string, int> connectFunc;
        public ConnectShare(Action<string, int> connectFunc)
        {
            InitializeComponent();
            this.connectFunc = connectFunc;
        }

        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                connectFunc(ConnectIP.Text, int.Parse(ConnectPort.Text));
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                MessageBox.Show(MainWindow.GetLocalizedString("ConnectFailed"), MainWindow.GetLocalizedString("Error"));
            }
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
