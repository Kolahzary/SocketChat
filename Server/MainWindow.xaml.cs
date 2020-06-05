using System.Windows;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatServer cs;

        public MainWindow()
        {
            this.InitializeComponent();

            this.lbActiveClients.SelectionChanged += (_s, _e) =>
            {
                if (this.lbActiveClients.SelectedValue == null)
                {
                    return;
                }

                if (this.lbActiveClients.SelectedValue is Client)
                {
                    this.tbTargetUsername.Text = (this.lbActiveClients.SelectedValue as Client).Username;
                }
            };

            this.DataContext = this.cs = new ChatServer();
        }

        private void bSend_Click(object sender, RoutedEventArgs e)
        {
            this.cs.SendMessage(this.tbTargetUsername.Text, this.tbMessage.Text);
        }

        private void bSwitchServerState_Click(object sender, RoutedEventArgs e)
        {
            this.cs.SwitchServerState();
        }
    }
}