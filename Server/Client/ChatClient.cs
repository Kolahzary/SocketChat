using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Client
{
    public class ChatClient : INotifyPropertyChanged
    {
        private Dispatcher _dispatcher;
        private Thread _thread;
        private Socket _socket;

        private IPAddress _ipAddress;
        public string IpAddress
        {
            get
            {
                return _ipAddress.ToString();
            }
            set
            {
                if (this.IsClientConnected)
                    throw new Exception("Can't change this property when server is active");
                _ipAddress = IPAddress.Parse(value);
            }
        }

        private ushort _port;
        public ushort Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (this.IsClientConnected)
                    throw new Exception("Can't change this property when server is active");
                this._port = value;
            }
        }


        private IPEndPoint _ipEndPoint => new IPEndPoint(_ipAddress, _port);

        private bool _isClientConnected;
        public bool IsClientConnected
        {
            get
            {
                return _isClientConnected;
            }
            private set
            {
                this._isClientConnected = value;

                this.NotifyPropertyChanged("IsClientConnected");
                this.NotifyPropertyChanged("IsClientDisconnected");
            }
        }
        public bool IsClientDisconnected => !this.IsClientConnected;

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
                if (this.IsClientConnected)
                {
                    this.SetUsername(value);
                }
            }
        }

        public BindingList<String> lstChat { get; set; }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion
        public ChatClient()
        {
            this._dispatcher = Dispatcher.CurrentDispatcher;
            this.lstChat = new BindingList<string>();

            this.IpAddress = "127.0.0.1";
            this.Port = 5960;
            this.Username = "Client" + new Random().Next(0, 99).ToString(); // random username
        }
        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
                return false;

            if (s.Available == 0)
                if (s.Poll(1000, SelectMode.SelectRead))
                    return false;

            return true;
        }
        public void SwitchClientState()
        {
            if (!this.IsClientConnected)
                this.Connect();
            else
                this.Disconnect();
        }
        private void Connect()
        {
            if (this.IsClientConnected) return;

            this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._socket.Connect(this._ipEndPoint);

            SetUsername(this.Username);

            this._thread = new Thread(() => this.ReceiveMessages());
            this._thread.Start();

            this.IsClientConnected = true;
        }
        private void Disconnect()
        {
            if (!this.IsClientConnected) return;
            if (this._socket != null && this._thread != null)
            {
                //this._thread.Abort(); MainThread = null;
                this._socket.Shutdown(SocketShutdown.Both);
                //this._socket.Disconnect(false);
                this._socket.Dispose();
                this._socket = null;
                this._thread = null;
            }
            this.lstChat.Clear();

            this.IsClientConnected = false;
        }
        public void ReceiveMessages()
        {
            while (true)
            {
                byte[] inf = new byte[1024];
                try
                {
                    if (!IsSocketConnected(this._socket))
                    {
                        this._dispatcher.Invoke(new Action(() =>
                        {
                            this.Disconnect();
                        }));
                        return;
                    }
                    int x = this._socket.Receive(inf);
                    if (x > 0)
                    {
                        string message = Encoding.Unicode.GetString(inf).Trim('\0');

                        this._dispatcher.Invoke(new Action(() =>
                        {
                            this.lstChat.Add(message);
                        }));
                    }
                }
                catch (Exception)
                {
                    this._dispatcher.Invoke(new Action(() =>
                    {
                        this.Disconnect();
                    }));

                    return;
                }
            }

        }

        private void SetUsername(string newUsername)
        {
            string cmd = string.Format("/setname {0}", newUsername);

            this._socket.Send(Encoding.Unicode.GetBytes(cmd));
        }
        public void SendMessageTo(string targetUsername, string message)
        {
            string cmd = string.Format("/msgto {0}:{1}", targetUsername, message);

            this._socket.Send(Encoding.Unicode.GetBytes(cmd));
        }
    }
}
