using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace SocketChat
{
    public class ChatServer : IChat
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

        public ChatServer()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            ClientList = new BindingList<Client>();
            ChatList = new BindingList<string>();
            
            this.ClientIdCounter = 0;
            this.SourceUsername = "Server";
        }

        public void StartConnection()
        {
            if (IsActive) // Is this nessercary?
            {
                return;
            }

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(IPEndPoint); // Put try catch here for multiple server on same endpoint
            Socket.Listen(5);

            Thread = new Thread(new ThreadStart(WaitForConnections));
            Thread.Start();

            ClientList.Add(new Client() { ID = 0, Username = SourceUsername });

            IsActive = true;
        }

        private void WaitForConnections()
        {
            while (true)
            {
                if (Socket == null)
                {
                    return;
                }

                Client client = new Client();
                client.ID = ClientIdCounter;
                client.Username = "NewUser"; // نام کاربری موقت
                try
                {
                    client.Socket = Socket.Accept();
                    client.Thread = new Thread(() => this.ProcessMessages(client));

                    Dispatcher.Invoke(new Action(() =>
                    {
                        ClientList.Add(client); //Share this list with Clients somehow
                        //this.SendClientListUpdate();
                    }), null);

                    client.Thread.Start();
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message, "Server Refused Client Connection");
                    //throw;
                }
            }
        }

        private void ProcessMessages(Client client)
        {
            while (true)
            {
                try
                {
                    if (!client.IsSocketConnected())
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            ClientList.Remove(client);
                            client.Dispose();
                        }), null);

                        return;
                    }

                    byte[] inf = new byte[1024];
                    int x = client.Socket.Receive(inf);
                    if (x > 0)
                    {
                        string strMessage = Encoding.Unicode.GetString(inf);
                        // check and execute commands
                        if (strMessage.Substring(0, 8) == "/setname")
                        {
                            string newUsername = strMessage.Replace("/setname ", "").Trim('\0');

                            client.Username = newUsername;
                        }
                        else if (strMessage.Substring(0, 6) == "/msgto")
                        {
                            string data = strMessage.Replace("/msgto ", "").Trim('\0');
                            string targetUsername = data.Substring(0, data.IndexOf(':'));
                            string message = data.Substring(data.IndexOf(':') + 1);

                            Dispatcher.Invoke(new Action(() =>
                            {
                                //this.SendMessage(client, targetUsername, message);
                                //this.SendMessage(targetUsername, message);
                                string message1 = client.Username + ": " + message;

                                // if target is server *TODO*

                                // Print recieved message
                                ChatList.Add(message1);

                                // Send to others

                                if (targetUsername != SourceUsername) // if message isnt going to server
                                {
                                    var c = ClientList.FirstOrDefault(i => i.Username == targetUsername);

                                    // if target username is registered
                                    if (c != null)
                                    {
                                        string message4 = client.Username + ": " + message;
                                        c.SendMessage(message4);
                                    }
                                    // if target username isn't registered
                                    else
                                    {
                                        string mess = "Error! Username not found, unable to deliver your message";
                                        client.SendMessage(mess);
                                        ChatList.Add("**Server**: Error! Username not found, unable to deliver your message");
                                    }
                                }

                            }), null);
                        }
                    }
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ClientList.Remove(client);
                        client.Dispose();
                    }), null);
                    return;
                }
            }
        }

        public void StopConnection()
        {
            if (!IsActive)
            {
                return;
            }

            //MainThread.Abort(); MainThread = null;
            //MainSocket.Shutdown(SocketShutdown.Both);

            ChatList.Clear();

            // remove all clients
            while (ClientList.Count != 0)
            {
                Client c = ClientList[0];

                ClientList.Remove(c);
                c.Dispose();
            }

            Socket.Dispose();
            Socket = null;

            IsActive = false;
        }

        public void OnIsActiveChanged(EventArgs e)
        {
            IsActiveChanged?.Invoke(this, e);
        }
    }
}