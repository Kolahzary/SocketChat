using System;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatClient cc;

        public MainWindow()
        {
            this.InitializeComponent();
            this.cc = new ChatClient();
            this.DataContext = this.cc;
        }

        private void bSend_Click(object sender, RoutedEventArgs e)
        {
            this.cc.SendMessageTo(this.tbTargetUsername.Text, this.tbMessage.Text);
        }

        private void bSwitchClientState_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.cc.SwitchClientState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}