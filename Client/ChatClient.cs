using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client
{
    public class ChatClient : INotifyPropertyChanged
    {
        private Dispatcher dispatcher;
        private IPAddress ipAddress;
        private bool isClientConnected;
        private ushort port;
        private Socket socket;
        private Thread thread;
        private string sourceUsername;
        private string targetUsername;
        private string messageContent;

        public ChatClient()
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.ChatList = new BindingList<string>();

            this.IpAddress = "127.0.0.1";
            this.Port = 5960;
            this.SourceUsername = "Client" + new Random().Next(0, 99).ToString(); // random username
            this.TargetUsername = "Server";

            this.SendMessageCMD = new RelayCommand(() => this.SendMessageTo(this.TargetUsername, this.MessageContent));
            this.SwitchClientStateCMD = new RelayCommand(this.SwitchClientState);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BindingList<string> ChatList { get; set; }

        public ICommand SendMessageCMD { get; }

        public ICommand SwitchClientStateCMD { get; }

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

        public string IpAddress
        {
            get
            {
                return this.ipAddress.ToString();
            }
            set
            {
                if (this.IsClientConnected)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.ipAddress = IPAddress.Parse(value);
            }
        }

        public bool IsClientConnected
        {
            get
            {
                return this.isClientConnected;
            }
            private set
            {
                this.isClientConnected = value;
                this.NotifyPropertyChanged("IsClientConnected");
                this.NotifyPropertyChanged("IsClientDisconnected");
            }
        }

        public bool IsClientDisconnected
        {
            get
            {
                return !this.IsClientConnected;
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
                if (this.IsClientConnected)
                {
                    throw new Exception("Can't change this property when server is active");
                }

                this.port = value;
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

                if (this.IsClientConnected)
                {
                    this.SetUsername(value);
                }
            }
        }

        private IPEndPoint ipEndPoint
        {
            get
            {
                return new IPEndPoint(this.ipAddress, this.port);
            }
        }

        public static bool IsSocketConnected(Socket s)
        {
            if (!s.Connected)
            {
                return false;
            }

            if (s.Available == 0)
            {
                if (s.Poll(1000, SelectMode.SelectRead))
                {
                    return false;
                }
            }

            return true;
        }

        public void ReceiveMessages()
        {
            while (true)
            {
                byte[] inf = new byte[1024];

                try
                {
                    if (!IsSocketConnected(this.socket))
                    {
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            this.Disconnect();
                        }));
                        return;
                    }

                    int x = this.socket.Receive(inf);

                    if (x > 0)
                    {
                        string message = Encoding.Unicode.GetString(inf).Trim('\0');

                        this.dispatcher.Invoke(new Action(() =>
                        {
                            this.ChatList.Add(message);
                        }));
                    }
                }
                catch (Exception)
                {
                    this.dispatcher.Invoke(new Action(() =>
                    {
                        this.Disconnect();
                    }));

                    return;
                }
            }
        }

        public void SendMessageTo(string targetUsername, string message)
        {
            string cmd = string.Format("/msgto {0}:{1}", targetUsername, message);

            this.socket.Send(Encoding.Unicode.GetBytes(cmd));
        }

        public void SwitchClientState()
        {
            if (!this.IsClientConnected)
            {
                try
                {
                    this.Connect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                this.Disconnect();
            }
        }

        private void Connect()
        {
            if (this.IsClientConnected)
            {
                return;
            }

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Connect(this.ipEndPoint);
            this.SetUsername(this.SourceUsername);
            this.thread = new Thread(() => this.ReceiveMessages());
            this.thread.Start();
            this.IsClientConnected = true;
        }

        private void Disconnect()
        {
            if (!this.IsClientConnected)
            {
                return;
            }

            if (this.socket != null && this.thread != null)
            {
                //this.thread.Abort(); MainThread = null;
                this.socket.Shutdown(SocketShutdown.Both);
                //this.socket.Disconnect(false);
                this.socket.Dispose();
                this.socket = null;
                this.thread = null;
            }

            this.ChatList.Clear();
            this.IsClientConnected = false;
        }

        private void NotifyPropertyChanged(string propName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void SetUsername(string newUsername)
        {
            string cmd = string.Format("/setname {0}", newUsername);

            this.socket.Send(Encoding.Unicode.GetBytes(cmd));
        }
    }
}