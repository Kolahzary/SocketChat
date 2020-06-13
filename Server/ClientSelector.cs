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
                return chatInterface.ChatList;
            }
            private set
            {
                chatInterface.ChatList = value;
                this.NotifyPropertyChanged("ChatList");
            }
        }
        public BindingList<Client> ClientList
        {
            get
            {
                return chatInterface.ClientList;
            }
            private set
            {
                chatInterface.ClientList = value;
                this.NotifyPropertyChanged("ClientList");
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
                // Add check = if there is an active connection setting can not be changed.
                this.isServer = value;
                this.NotifyPropertyChanged("IsServer");
                this.SelectClient();
            }
        }

        public bool IsActive
        {
            get
            {
                return chatInterface.IsActive;
            }
            private set
            {
                chatInterface.IsActive = value;
                this.NotifyPropertyChanged("IsActive");
                //this.NotifyPropertyChanged("IsServerActive");
                //this.NotifyPropertyChanged("IsServerStopped");
                //this.NotifyPropertyChanged("IsClientConnected");
                //this.NotifyPropertyChanged("IsClientDisconnected");
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
                return chatInterface.IPAddress.ToString();
            }
            set
            {
                if (IsActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                chatInterface.IPAddress = IPAddress.Parse(value);
            }
        }

        public ushort Port
        {
            get
            {
                return chatInterface.Port;
            }
            set
            {
                if (IsActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                chatInterface.Port = value;
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
                NotifyPropertyChanged("SourceUsername");
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
                // Change Icon.
                this.chatInterface = new ChatServer();

                SourceUsername = chatInterface.SourceUsername;
            }
            else
            {
                chatInterface = new ChatClient();

                SourceUsername = chatInterface.SourceUsername;
            }

            this.IpAddress = "127.0.0.1";
            this.Port = 5960;            

            ClientList = new BindingList<Client>();

            ChatList = new BindingList<string>();

            chatInterface.IsActiveChanged = new EventHandler(IsActiveBool);

            chatInterface.ClientList.ListChanged += (_sender, _e) =>
            {
                this.NotifyPropertyChanged("ActiveClients");
            };

        }

        public void StartConnection()
        {
            if (IsServer)
            {
                if (!IsActive)
                {
                    chatInterface.StartConnection();
                }
                else // server is currently active and should be stopped
                {
                    chatInterface.StopConnection();
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
            string sourceMessage = chatInterface.SourceUsername + ": " + messageContent;
            chatInterface.ChatList.Add(sourceMessage);

            bool isSent = false;

            if (targetUsername == this.SourceUsername)
            {

                isSent = true;
            }
            else
            {
                //Server send
                if (IsServer)
                {
                    var client = chatInterface.ClientList.First(i => i.Username == TargetUsername);
                    client.SendMessage(sourceMessage);
                    isSent = true;
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

        }

        public void IsActiveBool(object sender, EventArgs e)
        {
            NotifyPropertyChanged("IsActive");
        }

        public void UpdateChat(object sender, EventArgs e)
        {
            NotifyPropertyChanged("ChatList");
        }

        private void UpdateUsers()
        {
            Client c = chatInterface.ClientList.First(i => i.Username == TargetUsername);
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
