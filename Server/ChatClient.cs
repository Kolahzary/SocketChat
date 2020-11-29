using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
                return this.isActive;
            }
            set
            {
                this.isActive = value;
                this.OnIsActiveChanged(EventArgs.Empty);
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
            this.Dispatcher = Dispatcher.CurrentDispatcher;
            this.ClientList = new BindingList<Client>();
            this.ChatList = new BindingList<string>();

            this.ClientIdCounter = 0;
            this.SourceUsername = "Client" + new Random().Next(0, 99).ToString(); // random username
        }

        public void StartConnection()
        {
            if (this.IsActive)
            {
                return;
            }

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.Connect(this.IPEndPoint);
            this.SetUsername(this.SourceUsername);

            this.Thread = new Thread(() => this.ReceiveMessages());
            this.Thread.Start();

            this.IsActive = true;
        }

        private void SetUsername(string newUsername)
        {
            string cmd = string.Format("/setname {0}", newUsername);

            this.Socket.Send(Encoding.Unicode.GetBytes(cmd));
        }

        public void ReceiveMessages()
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
                            this.StopConnection();
                        }));
                        return;
                    }

                    int x = this.Socket.Receive(inf);

                    if (x > 0)
                    {
                        string strMessage = Encoding.Unicode.GetString(inf);

                        // Adds new users to existing client list
                        if (strMessage.Substring(0, 8) == "/setname")
                        {
                            string newUsername = strMessage.Replace("/setname ", "").Trim('\0');

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.ClientList.Add(new Client() { ID = ClientIdCounter, Username = newUsername });
                            }));

                        }
                        // Removes old users from existing client list
                        else if (strMessage.Substring(0, 8) == "/delname")
                        {
                            string oldUsername = strMessage.Replace("/delname ", "").Trim('\0');

                            Client oldUser = this.ClientList.First(item => item.Username == oldUsername);

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.ClientList.Remove(oldUser);
                            }));
                        }
                        else
                        {
                            strMessage = Encoding.Unicode.GetString(inf).Trim('\0');

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.ChatList.Add(strMessage);
                            }));
                    }
                }
                }
                catch (SocketException ex)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.StopConnection();

                        // Concurrently closing a listener that is accepting at the time causes exception 10004.
                        Debug.WriteLineIf(ex.ErrorCode != 10004, $"*EXCEPTION* {ex.ErrorCode}: {ex.Message}");
                        if (ex.ErrorCode != 10004)
                        {
                            MessageBox.Show(ex.Message);
                        }
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

        public void StopConnection()
        {
            if (!this.IsActive)
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

            this.ChatList.Clear();
            this.ClientList.Clear();
            this.IsActive = false;
        }

        public void OnIsActiveChanged(EventArgs e)
        {
            this.IsActiveChanged?.Invoke(this, e);
        }
    }
}