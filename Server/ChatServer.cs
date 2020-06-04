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
using System.Windows.Threading;

namespace Server
{
    public class ChatServer : INotifyPropertyChanged
    {
        private Dispatcher _dispatcher { get; set; }

        private int _clientIdCounter;
        private Thread _thread;
        private Socket _socket;

        private IPAddress _ipAddress;
        private ushort _port;
        public ushort Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (this.IsServerActive)
                    throw new Exception("Can't change this property when server is active");
                this._port = value;
            }
        }

        private IPEndPoint _ipEndPoint => new IPEndPoint(_ipAddress, _port);

        public string IpAddress
        {
            get
            {
                return _ipAddress.ToString();
            }
            set
            {
                if (this.IsServerActive)
                    throw new Exception("Can't change this property when server is active");
                _ipAddress = IPAddress.Parse(value);
            }
        }

        private string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                this._username = value;
                if (this.IsServerActive)
                {
                    this.lstClients[0].Username = value;
                }
            }
        }

        public BindingList<Client> lstClients { get; set; }
        public BindingList<String> lstChat { get; set; }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion

        private bool _isServerActive;
        public bool IsServerActive
        {
            get
            {
                return _isServerActive;
            }
            private set
            {
                this._isServerActive = value;

                this.NotifyPropertyChanged("IsServerActive");
                this.NotifyPropertyChanged("IsServerStopped");
            }
        }
        public bool IsServerStopped => !this.IsServerActive;
        public int ActiveClients => lstClients.Count;

        public ChatServer()
        {
            this._dispatcher = Dispatcher.CurrentDispatcher;
            this.lstChat = new BindingList<string>();
            this.lstClients = new BindingList<Client>();
            this.lstClients.ListChanged += (_sender, _e) =>
              {
                  this.NotifyPropertyChanged("ActiveClients");
              };

            this._clientIdCounter = 0;
            this.IpAddress = "127.0.0.1";
            this.Port = 5960;
            this.Username = "Server";
        }
        public void StartServer()
        {
            if (this.IsServerActive) return;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(_ipEndPoint);
            _socket.Listen(5);

            _thread = new Thread(new ThreadStart(WaitForConnections));
            _thread.Start();

            lstClients.Add(new Client() { ID = 0, Username = this.Username });

            this.IsServerActive = true;
        }
        public void StopServer()
        {
            if (!this.IsServerActive) return;

            //MainThread.Abort(); MainThread = null;
            //MainSocket.Shutdown(SocketShutdown.Both);

            lstChat.Clear();

            // remove all clients
            while (lstClients.Count != 0)
            {
                Client c = lstClients[0];

                lstClients.Remove(c);
                c.Dispose();
            }

            _socket.Dispose();
            _socket = null;

            this.IsServerActive = false;
        }
        public void SwitchServerState()
        {
            if (!this.IsServerActive) // server should be activated
                this.StartServer();
            else // server is currently active and should be stopped
                this.StopServer();
        }
        public void SendMessage(string toUsername, string messageContent)
            => this.SendMessage(this.lstClients[0], toUsername, messageContent);
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
            foreach (Client c in lstClients)
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

        void WaitForConnections()
        {
            while (true)
            {
                if (_socket == null) return;
                Client client = new Client();
                client.ID = this._clientIdCounter;
                client.Username = "NewUser"; // نام کاربری موقت
                try
                {
                    client.Socket = _socket.Accept();
                    client.Thread = new Thread(() => ProcessMessages(client));

                    this._dispatcher.Invoke(new Action(() =>
                    {
                        lstClients.Add(client);
                    }), null);

                    client.Thread.Start();
                }
                catch (Exception)
                {
                    //MessageBox.Show(ex.Message, "Error");
                }
            }
        }
        void ProcessMessages(Client c)
        {
            while (true)
            {
                try
                {
                    if (!c.IsSocketConnected())
                    {
                        this._dispatcher.Invoke(new Action(() =>
                        {
                            lstClients.Remove(c);
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
                                SendMessage(c, targetUsername, message);
                            }), null);
                        }

                    }
                }
                catch (Exception)
                {
                    this._dispatcher.Invoke(new Action(() =>
                    {
                        lstClients.Remove(c);
                        c.Dispose();
                    }), null);
                    return;
                }
            }
        }
    }
}
