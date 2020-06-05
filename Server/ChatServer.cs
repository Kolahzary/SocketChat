using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace Server
{
    public class ChatServer : INotifyPropertyChanged
    {
        private int clientIdCounter;
        private IPAddress ipAddress;
        private bool isServerActive;
        private ushort port;
        private Socket socket;
        private Thread thread;
        private string username;

        public ChatServer()
        {
            this._dispatcher = Dispatcher.CurrentDispatcher;
            this.lstChat = new BindingList<string>();
            this.lstClients = new BindingList<Client>();
            this.lstClients.ListChanged += (_sender, _e) =>
            {
                this.NotifyPropertyChanged("ActiveClients");
            };

            this.clientIdCounter = 0;
            this.IpAddress = "127.0.0.1";
            this.Port = 5960;
            this.Username = "Server";
        }

        public int ActiveClients
        {
            get
            {
                return this.lstClients.Count;
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

        public BindingList<String> lstChat { get; set; }

        public BindingList<Client> lstClients { get; set; }

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

        public string Username
        {
            get
            {
                return this.username;
            }
            set
            {
                this.username = value;
                if (this.IsServerActive)
                {
                    this.lstClients[0].Username = value;
                }
            }
        }

        private Dispatcher _dispatcher { get; set; }

        private IPEndPoint _ipEndPoint
        {
            get
            {
                return new IPEndPoint(this.ipAddress, this.port);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void SendMessage(string toUsername, string messageContent)
        {
            this.SendMessage(this.lstClients[0], toUsername, messageContent);
        }

        public void StartServer()
        {
            if (this.IsServerActive)
            {
                return;
            }

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Bind(this._ipEndPoint);
            this.socket.Listen(5);

            this.thread = new Thread(new ThreadStart(this.WaitForConnections));
            this.thread.Start();

            this.lstClients.Add(new Client() { ID = 0, Username = this.Username });

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

            this.lstChat.Clear();

            // remove all clients
            while (this.lstClients.Count != 0)
            {
                Client c = this.lstClients[0];

                this.lstClients.Remove(c);
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
        private void ProcessMessages(Client c)
        {
            while (true)
            {
                try
                {
                    if (!c.IsSocketConnected())
                    {
                        this._dispatcher.Invoke(new Action(() =>
                        {
                            this.lstClients.Remove(c);
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

                            this._dispatcher.Invoke(new Action(() =>
                            {
                                this.SendMessage(c, targetUsername, message);
                            }), null);
                        }
                    }
                }
                catch (Exception)
                {
                    this._dispatcher.Invoke(new Action(() =>
                    {
                        this.lstClients.Remove(c);
                        c.Dispose();
                    }), null);
                    return;
                }
            }
        }

        private void SendMessage(Client from, string toUsername, string messageContent)
        {
            string logMessage = string.Format("**log** From {0} | To {1} | Message {2}", from.Username, toUsername, messageContent);
            this.lstChat.Add(logMessage);

            string message = from.Username + ": " + messageContent;

            bool isSent = false;

            // if target is server
            if (toUsername == this.Username)
            {
                this.lstChat.Add(message);
                isSent = true;
            }

            // if target username is registered
            foreach (Client c in this.lstClients)
            {
                if (c.Username == toUsername)
                {
                    c.SendMessage(message);
                    isSent = true;
                }
            }

            // if target username isn't registered
            if (!isSent)
            {
                from.SendMessage("**Server**: Error! Username not found, unable to deliver your message"); // send an error to sender
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

                    this._dispatcher.Invoke(new Action(() =>
                    {
                        this.lstClients.Add(client);
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