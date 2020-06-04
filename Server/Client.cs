using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Client : IDisposable, INotifyPropertyChanged
    {
        private int _id;
        private string _username;

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                this._id = value;
                this.NotifyPropertyChanged("ID");
            }
        }
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                this._username = value;
                this.NotifyPropertyChanged("Username");
            }
        }
        public Socket Socket { get; set; }
        public Thread Thread { get; set; }


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
        
        public bool IsSocketConnected()
        {
            return IsSocketConnected(Socket);
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

        #region IDisposable implementation
        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (this.Socket != null)
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                    this.Socket.Dispose();
                    this.Socket = null;
                }
                if (this.Thread != null)
                    this.Thread = null;
                _isDisposed = true;
            }
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion
    }
}
