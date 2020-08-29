using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SocketChat
{
    public class ClientSelector : INotifyPropertyChanged
    {
        private bool isServer;
        private IChat chatInterface;
        private Client selectedUsername;
        private string targetUsername;
        private string messageContent;

        public ClientSelector()
        {
            this.IsServer = true;

            this.StartConnectionCMD = new RelayCommand(this.StartConnection);
            this.SendMessageCMD = new RelayCommand(() => this.SendMessage(this.TargetUsername, this.MessageContent));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand StartConnectionCMD { get; }
        public ICommand SendMessageCMD { get; }

        public BindingList<String> ChatList
        {
            get
            {
                return this.chatInterface.ChatList;
            }
            private set
            {
                this.chatInterface.ChatList = value;
                this.NotifyPropertyChanged("ChatList");
            }
        }
        public BindingList<Client> ClientList
        {
            get
            {
                return this.chatInterface.ClientList;
            }
            private set
            {
                this.chatInterface.ClientList = value;
                this.NotifyPropertyChanged("ClientList");
            }
        }

        public string WindowTitle
        {
            get
            {
                if (IsServer)
                {
                    return "Server";
                }
                else
                {
                    return "Client";
                }
            }
        }

        public string WindowIcon
        {
            get
            {
                if (IsServer)
                {
                    return "Server.ico";
                }
                else
                {
                    return "Client.ico";
                }
            }
        }

        public bool IsServer
        {
            get
            {
                return this.isServer;
            }
            set
            {
                this.isServer = value;
                this.NotifyPropertyChanged("IsServer");
                this.NotifyPropertyChanged("WindowTitle");
                this.NotifyPropertyChanged("WindowIcon");
                this.SelectClient();
            }
        }

        public bool IsActive
        {
            get
            {
                return this.chatInterface.IsActive;
            }
            private set
            {
                this.chatInterface.IsActive = value;
                this.NotifyPropertyChanged("IsActive");
            }
        }

        public int ActiveClients
        {
            get
            {
                return this.chatInterface.ClientList.Count;
            }
        }

        public string IpAddress
        {
            get
            {
                return this.chatInterface.IPAddress.ToString();
            }
            set
            {
                if (this.IsActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.chatInterface.IPAddress = IPAddress.Parse(value);
                this.NotifyPropertyChanged("IpAddress");
            }
        }

        public ushort Port
        {
            get
            {
                return this.chatInterface.Port;
            }
            set
            {
                if (this.IsActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.chatInterface.Port = value;
                this.NotifyPropertyChanged("Port");
            }
        }

        public string SourceUsername
        {
            get
            {
                return this.chatInterface.SourceUsername;
            }
            set
            {
                this.chatInterface.SourceUsername = value;
                if (this.IsActive)
                {
                    this.chatInterface.ClientList[0].Username = value;
                }
                this.NotifyPropertyChanged("SourceUsername");
            }
        }

        public string TargetUsername
        {
            get
            {
                return this.targetUsername;
            }
            set
            {
                this.targetUsername = value;
                this.NotifyPropertyChanged("TargetUsername");
            }
        }

        public Client SelectedUsername
        {
            get
            {
                return this.selectedUsername;
            }
            set
            {
                this.selectedUsername = value;

                if (this.selectedUsername == null)
                {
                    return;
                }

                if (this.selectedUsername is Client)
                {
                    this.TargetUsername = this.selectedUsername.Username.ToString();
                }

                this.NotifyPropertyChanged("SelectedUsername");
            }
        }

        public string MessageContent
        {
            get
            {
                return this.messageContent;
            }
            set
            {
                this.messageContent = value;
                this.NotifyPropertyChanged("MessageContent");
            }
        }

        private void SelectClient()
        {
            if (this.IsServer)
            {
                this.chatInterface = new ChatServer();
                this.SourceUsername = this.chatInterface.SourceUsername;
            }
            else
            {
                this.chatInterface = new ChatClient();
                this.SourceUsername = this.chatInterface.SourceUsername;
            }

            this.IpAddress = "127.0.0.1";
            this.Port = 5960;

            this.ClientList = new BindingList<Client>();

            this.ChatList = new BindingList<string>();

            this.chatInterface.IsActiveChanged = new EventHandler(this.IsActiveBool);

            this.chatInterface.ClientList.ListChanged += (_sender, _e) =>
            {
                this.NotifyPropertyChanged("ActiveClients");
            };

        }

        public void StartConnection()
        {
            if (this.IsServer)
            {
                if (!this.IsActive)
                {
                    try
                    {
                        this.chatInterface.StartConnection();
                    }
                    catch (SocketException ex)
                    {
                        if (ex.ErrorCode == 10048)
                        {
                            MessageBox.Show("Port " + Port + " is currently in use.");
                        }
                        else
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                else
                {
                    this.chatInterface.StopConnection();
                }
            }
            else
            {
                if (!this.IsActive)
                {
                    try
                    {
                        this.chatInterface.StartConnection();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    this.chatInterface.StopConnection();
                }
            }
        }

        public void SendMessage(string targetUsername, string messageContent)
        {
            string sourceMessage = this.chatInterface.SourceUsername + ": " + messageContent;

            bool isSent = false;

            if (targetUsername == this.SourceUsername)
            {

                isSent = true;
            }
            else
            {
                //Server send
                if (this.IsServer)
                {
                    Client client = this.chatInterface.ClientList.FirstOrDefault(i => i.Username == this.TargetUsername);
                    if (client != null)
                    {
                        client.SendMessage(sourceMessage);
                        isSent = true;
                    }
                    else
                    {
                        sourceMessage = targetUsername + ": " + "is not an active client.";
                        isSent = false;
                    }
                }
                //Client Send
                else
                {
                    string message = string.Format("/msgto {0}:{1}", targetUsername, messageContent);
                    this.chatInterface.Socket.Send(Encoding.Unicode.GetBytes(message)); // check this

                    //client.SendMessage(targetUsername, MessageContent);

                    isSent = true;
                }
            }

            this.chatInterface.ChatList.Add(sourceMessage);
        }

        public void IsActiveBool(object sender, EventArgs e)
        {
            this.NotifyPropertyChanged("IsActive");
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
