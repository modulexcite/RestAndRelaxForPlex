using JimBobBennett.JimLib.Events;
using JimBobBennett.JimLib.Mvvm;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class ServerConnection : NotificationObject
    {
        private ConnectionStatus _connectionStatus;
        
        public ServerConnection(IPlexServerConnection plexServerConnection)
        {
            Title = plexServerConnection.Name;
            ConnectionStatus = plexServerConnection.ConnectionStatus;
            plexServerConnection.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void OnConnectionStatusChanged(object sender, EventArgs<ConnectionStatus> eventArgs)
        {
            ConnectionStatus = ((IPlexServerConnection)sender).ConnectionStatus;
        }

        public string Title { get; private set; }

        public ConnectionStatus ConnectionStatus
        {
            get { return _connectionStatus; }
            internal set
            {
                if (_connectionStatus == value) return;

                _connectionStatus = value;
                RaisePropertyChanged();
            }
        }
    }
}
