using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SocketChat
{
    public class ChatClient : IChat
    {
        private bool isActive;

        public Socket Socket { get; set; }
        public Thread Thread { get; set; }
        public Dispatcher Dispatcher { get; set; }

        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                OnIsActiveChanged(EventArgs.Empty);
            }
        }

        public IPAddress IPAddress { get; set; }
        public ushort Port { get; set; }
        public IPEndPoint IPEndPoint { get { return new IPEndPoint(this.IPAddress, this.Port); } }
        public int ClientIdCounter { get; set; }
        public string SourceUsername { get; set; }

        public BindingList<Client> ClientList { get; set; }
        public BindingList<string> ChatList { get; set; }

        public EventHandler IsActiveChanged { get; set; }

        public ChatClient()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            ClientList = new BindingList<Client>();
            ChatList = new BindingList<string>();

            this.ClientIdCounter = 0;
            this.SourceUsername = "Client" + new Random().Next(0, 99).ToString(); // random username
        }

        public void StartConnection()
        {
            if (IsActive)
            {
                return;
            }

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Connect(IPEndPoint);
            SetUsername(SourceUsername);

            Thread = new Thread(() => this.ReceiveMessages());
            Thread.Start();

            ClientList.Add(new Client() { ID = 0, Username = SourceUsername }); // Test Added from Server, Adds your self

            IsActive = true;

            //this.GetClientListUpdate();
        }

        private void SetUsername(string newUsername)
        {
            string cmd = string.Format("/setname {0}", newUsername);

            this.Socket.Send(Encoding.Unicode.GetBytes(cmd));
        }

        public void ReceiveMessages() // Client
        {
            while (true)
            {
                byte[] inf = new byte[1024];

                try
                {
                    if (!IsSocketConnected(this.Socket))
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            StopConnection();
                        }));
                        return;
                    }

                    int x = this.Socket.Receive(inf);

                    if (x > 0)
                    {
                        string message = Encoding.Unicode.GetString(inf).Trim('\0');

                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            ChatList.Add(message);
                        }));
                    }
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.StopConnection();
                    }));

                    return;
                }
            }
        }

        public static bool IsSocketConnected(Socket socket)
        {
            if (!socket.Connected)
            {
                return false;
            }

            if (socket.Available == 0)
            {
                if (socket.Poll(1000, SelectMode.SelectRead))
                {
                    return false;
                }
            }

            return true;
        }

        //public void SendMessage(string target, string message)
        //{
        //    string message1 = string.Format("/msgto {0}:{1}", target, message);
        //    this.socket.Send(Encoding.Unicode.GetBytes(message1));
        //}

        public void StopConnection()
        {
            if (!IsActive)
            {
                return;
            }

            if (this.Socket != null && this.Thread != null)
            {
                //this.thread.Abort(); MainThread = null;
                this.Socket.Shutdown(SocketShutdown.Both);
                //this.socket.Disconnect(false);
                this.Socket.Dispose();
                this.Socket = null;
                this.Thread = null;
            }

            ChatList.Clear();
            IsActive = false;
        }

        public void OnIsActiveChanged(EventArgs e)
        {
            IsActiveChanged?.Invoke(this, e);
        }
    }
}