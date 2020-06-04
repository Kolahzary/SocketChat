using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ChatServer cs;
        public MainWindow()
        {
            InitializeComponent();

            lbActiveClients.SelectionChanged += (_s,_e)=>
            {
                if (lbActiveClients.SelectedValue == null)
                    return;
                if (lbActiveClients.SelectedValue is Client)
                    tbTargetUsername.Text = (lbActiveClients.SelectedValue as Client).Username;
            };


            this.DataContext = cs = new ChatServer();
        }

        private void bSwitchServerState_Click(object sender, RoutedEventArgs e)
        {
            cs.SwitchServerState();
        }

        private void bSend_Click(object sender, RoutedEventArgs e)
        {
            cs.SendMessage(tbTargetUsername.Text, tbMessage.Text);
        }
    }
}
