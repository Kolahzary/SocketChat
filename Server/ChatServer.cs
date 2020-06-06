using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace Server
{
    public class ChatServer : INotifyPropertyChanged
    {
        private int clientIdCounter;
        private IPAddress ipAddress;
        private bool isServerActive;
        private ushort port;
        private Client selectedUsername;
        private Socket socket;
        private string messageContent;
        private string targetUsername;
        private Thread thread;
        private string sourceUsername;

        public ChatServer()
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.ChatList = new BindingList<string>();
            this.ClientList = new BindingList<Client>();
            this.ClientList.ListChanged += (_sender, _e) =>
            {
                this.NotifyPropertyChanged("ActiveClients");
            };

            this.clientIdCounter = 0;
            this.IpAddress = "127.0.0.1";
            this.Port = 5960;
            this.SourceUsername = "Server";

            this.SendMessageCMD = new RelayCommand(() => this.SendMessage(this.TargetUsername, this.MessageContent));
            this.SwitchServerStateCMD = new RelayCommand(this.SwitchServerState);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BindingList<String> ChatList { get; set; }

        public BindingList<Client> ClientList { get; set; }

        public ICommand SendMessageCMD { get; }

        public ICommand SwitchServerStateCMD { get; }

        public int ActiveClients
        {
            get
            {
                return this.ClientList.Count;
            }
        }

        public string IpAddress
        {
            get
            {
                return this.ipAddress.ToString();
            }
            set
            {
                if (this.IsServerActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.ipAddress = IPAddress.Parse(value);
            }
        }

        public bool IsServerActive
        {
            get
            {
                return this.isServerActive;
            }
            private set
            {
                this.isServerActive = value;
                this.NotifyPropertyChanged("IsServerActive");
                this.NotifyPropertyChanged("IsServerStopped");
            }
        }

        public bool IsServerStopped
        {
            get
            {
                return !this.IsServerActive;
            }
        }

        public ushort Port
        {
            get
            {
                return this.port;
            }
            set
            {
                if (this.IsServerActive)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.port = value;
            }
        }

        public Client SelectedUsername
        {
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
        public string SourceUsername
        {
            get
            {
                return this.sourceUsername;
            }
            set
            {
                this.sourceUsername = value;
                if (this.IsServerActive)
                {
                    this.ClientList[0].Username = value;
                }
            }
        }

        private Dispatcher dispatcher { get; set; }

        private IPEndPoint ipEndPoint
        {
            get
            {
                return new IPEndPoint(this.ipAddress, this.port);
            }
        }
        public void SendMessage(string targetUsername, string messageContent)
        {
            this.SendMessage(this.ClientList[0], targetUsername, messageContent);
        }

        public void StartServer()
        {
            if (this.IsServerActive)
            {
                return;
            }

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Bind(this.ipEndPoint);
            this.socket.Listen(5);

            this.thread = new Thread(new ThreadStart(this.WaitForConnections));
            this.thread.Start();

            this.ClientList.Add(new Client() { ID = 0, Username = this.SourceUsername });

            this.IsServerActive = true;
        }

        public void StopServer()
        {
            if (!this.IsServerActive)
            {
                return;
            }

            //MainThread.Abort(); MainThread = null;
            //MainSocket.Shutdown(SocketShutdown.Both);

            this.ChatList.Clear();

            // remove all clients
            while (this.ClientList.Count != 0)
            {
                Client c = this.ClientList[0];

                this.ClientList.Remove(c);
                c.Dispose();
            }

            this.socket.Dispose();
            this.socket = null;

            this.IsServerActive = false;
        }

        public void SwitchServerState()
        {
            if (!this.IsServerActive) // server should be activated
            {
                this.StartServer();
            }
            else // server is currently active and should be stopped
            {
                this.StopServer();
            }
        }

        private void NotifyPropertyChanged(string propName)
        {
            //this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        private void ProcessMessages(Client c)
        {
            while (true)
            {
                try
                {
                    if (!c.IsSocketConnected())
                    {
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            this.ClientList.Remove(c);
                            c.Dispose();
                        }), null);

                        return;
                    }

                    byte[] inf = new byte[1024];
                    int x = c.Socket.Receive(inf);
                    if (x > 0)
                    {
                        string strMessage = Encoding.Unicode.GetString(inf);
                        // check and execute commands
                        if (strMessage.Substring(0, 8) == "/setname")
                        {
                            string newUsername = strMessage.Replace("/setname ", "").Trim('\0');

                            c.Username = newUsername;
                        }
                        else if (strMessage.Substring(0, 6) == "/msgto")
                        {
                            string data = strMessage.Replace("/msgto ", "").Trim('\0');
                            string targetUsername = data.Substring(0, data.IndexOf(':'));
                            string message = data.Substring(data.IndexOf(':') + 1);

                            this.dispatcher.Invoke(new Action(() =>
                            {
                                this.SendMessage(c, targetUsername, message);
                            }), null);
                        }
                    }
                }
                catch (Exception)
                {
                    this.dispatcher.Invoke(new Action(() =>
                    {
                        this.ClientList.Remove(c);
                        c.Dispose();
                    }), null);
                    return;
                }
            }
        }

        private void SendMessage(Client client, string targetUsername, string messageContent)
        {
            string logMessage = string.Format("**log** From {0} | To {1} | Message {2}", client.Username, targetUsername, messageContent);
            this.ChatList.Add(logMessage);

            string message = client.Username + ": " + messageContent;

            bool isSent = false;

            // if target is server
            if (targetUsername == this.SourceUsername)
            {
                this.ChatList.Add(message);
                isSent = true;
            }

            // if target username is registered
            foreach (Client c in this.ClientList)
            {
                if (c.Username == targetUsername)
                {
                    c.SendMessage(message);
                    isSent = true;
                }
            }

            // if target username isn't registered
            if (!isSent)
            {
                client.SendMessage("**Server**: Error! Username not found, unable to deliver your message"); // send an error to sender
            }
        }

        private void WaitForConnections()
        {
            while (true)
            {
                if (this.socket == null)
                {
                    return;
                }

                Client client = new Client();
                client.ID = this.clientIdCounter;
                client.Username = "NewUser"; // نام کاربری موقت
                try
                {
                    client.Socket = this.socket.Accept();
                    client.Thread = new Thread(() => this.ProcessMessages(client));

                    this.dispatcher.Invoke(new Action(() =>
                    {
                        this.ClientList.Add(client);
                    }), null);

                    client.Thread.Start();
                }
                catch (Exception)
                {
                    //MessageBox.Show(ex.Message, "Error");
                }
            }
        }
    }
}