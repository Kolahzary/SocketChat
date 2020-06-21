using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
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

        public ChatServer()
        {
            this.Dispatcher = Dispatcher.CurrentDispatcher;
            this.ClientList = new BindingList<Client>();
            this.ChatList = new BindingList<string>();

            this.ClientIdCounter = 0;
            this.SourceUsername = "Server";


        }

        public void StartConnection()
        {
            if (this.IsActive) // Is this nessercary?
            {
                return;
            }

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Socket.Bind(this.IPEndPoint);
            this.Socket.Listen(5);

            this.Thread = new Thread(new ThreadStart(this.WaitForConnections));
            this.Thread.Start();

            this.ClientList.Add(new Client() { ID = ClientIdCounter, Username = SourceUsername });
            //ClientIdCounter++;

            this.IsActive = true;
        }

        private void WaitForConnections()
        {
            while (true)
            {
                if (this.Socket == null)
                {
                    return;
                }

                Client client = new Client();
                client.ID = this.ClientIdCounter;
                client.Username = "NewUser"; // نام کاربری موقت
                try
                {
                    client.Socket = this.Socket.Accept();
                    client.Thread = new Thread(() => this.ProcessMessages(client)); // maybe add await
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.ClientList.Add(client);
                    }), null);

                    client.Thread.Start();                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "waitForConnections Method - Server Refused Client Connection");
                    //throw new Exception(ex.Message);
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
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.RemoveActiveUser(client);
                            this.ClientList.Remove(client);
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

                            // Updates Clients with new and existing user list.
                            this.UpdateClientsActiveUsers(client, strMessage);
                        }
                        // delete this
                        //if (x == 0)
                        //{
                        //    MessageBox.Show("X == 0, new error!");
                        //}
                        else if (strMessage.Substring(0, 6) == "/msgto")
                        {
                            string data = strMessage.Replace("/msgto ", "").Trim('\0');
                            string targetUsername = data.Substring(0, data.IndexOf(':'));
                            string message = data.Substring(data.IndexOf(':') + 1);

                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                string targetMessage = client.Username + ": " + message;

                                // if target is server *TODO*

                                // Print recieved message
                                this.ChatList.Add(targetMessage);

                                // Forwards message to clients
                                this.ForwardClientMessage(client, targetUsername, targetMessage);


                            }), null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        Debug.WriteLine(ex.ToString());

                        this.RemoveActiveUser(client);
                        this.ClientList.Remove(client);
                        client.Dispose();
                    }), null);
                    return;
                }
            }
        }

        private void UpdateClientsActiveUsers(Client client, string strMessage)
        {
            foreach (Client user in this.ClientList)
            {
                // Sends all existing users to the most recent user.
                if (client == this.ClientList.Last())
                {
                    string allUsers = $"/setname {user.Username}";
                    client.SendMessage(allUsers);

                    // Async may send names as one string if not set.
                    Thread.Sleep(50);
                }

                // Sends most recent user to all existing users.
                if (client != user)
                {
                    this.ForwardClientMessage(client, user.Username, strMessage);
                }
            }
        }

        private void RemoveActiveUser(Client client)
        {
            string delMessage = string.Format("/delname {0}", client.Username);

            foreach (Client user in this.ClientList)
            {
                if (client != user)
                {
                    this.ForwardClientMessage(client, user.Username, delMessage);
                }
            }            
        }

        private void ForwardClientMessage(Client client, string targetUsername, string targetMessage)
        {
            // Stops server sending message to itself.
            if (targetUsername != this.SourceUsername)
            {
                Client user = this.ClientList.FirstOrDefault(item => item.Username == targetUsername);

                // if target username is registered
                if (user != null)
                {
                    user.SendMessage(targetMessage);
                }
                // if target username isn't registered
                else
                {
                    string errorMessage = "Error! Username not found, unable to deliver your message";
                    client.SendMessage(errorMessage);
                    this.ChatList.Add("**Server**: Error! Username not found, unable to deliver your message");
                }
            }
        }

        public void StopConnection()
        {
            if (!this.IsActive)
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

            this.Socket.Dispose();
            this.Socket = null;

            
            this.IsActive = false;
        }

        public void OnIsActiveChanged(EventArgs e)
        {
            this.IsActiveChanged?.Invoke(this, e);
        }
    }
}