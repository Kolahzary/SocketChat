using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketChat
{
    public class Client : IDisposable, INotifyPropertyChanged
    {
        private int id;
        private bool isDisposed;
        private string username;

        public event PropertyChangedEventHandler PropertyChanged;

        public Socket Socket { get; set; }

        public Thread Thread { get; set; }

        public int ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
                this.NotifyPropertyChanged("ID");
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
                this.NotifyPropertyChanged("Username");
            }
        }

        public Client()
        {
            this.isDisposed = false;
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

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                if (this.Socket != null)
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                    this.Socket.Dispose();
                    this.Socket = null;
                }
                if (this.Thread != null)
                {
                    this.Thread = null;
                }

                this.isDisposed = true;
            }
        }

        public bool IsSocketConnected()
        {
            return IsSocketConnected(this.Socket);
        }

        public void SendMessage(string message)
        {
            try
            {
                this.Socket.Send(Encoding.Unicode.GetBytes(message));
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private void NotifyPropertyChanged(string propName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}