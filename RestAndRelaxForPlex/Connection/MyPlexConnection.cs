using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.JimLib.Xamarin.Network;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
// ReSharper disable once ClassNeverInstantiated.Global
    public class MyPlexConnection : IMyPlexConnection
    {
        private readonly IRestConnection _restConnection;
        
        public PlexUser User { get; private set; }

        private readonly ObservableCollectionEx<Device> _devices;
        private readonly ObservableCollectionEx<Device> _servers;
        private readonly ObservableCollectionEx<Device> _players;

        public ReadOnlyObservableCollection<Device> Devices { get; private set; }
        public ReadOnlyObservableCollection<Device> Servers { get; private set; }
        public ReadOnlyObservableCollection<Device> Players { get; private set; }

        private string _username;
        private string _password;

        private readonly object _deviceSyncObj = new object();
        private readonly object _tokenSyncObj = new object();

        public MyPlexConnection(IRestConnection restConnection)
        {
            _restConnection = restConnection;
            _devices = new ObservableCollectionEx<Device>();
            _servers = new ObservableCollectionEx<Device>();
            _players = new ObservableCollectionEx<Device>();

            Devices = new ReadOnlyObservableCollection<Device>(_devices);
            Servers = new ReadOnlyObservableCollection<Device>(_servers);
            Players = new ReadOnlyObservableCollection<Device>(_players);
        }

        public async Task ConnectAsync(string username, string password)
        {
            if (!IsConnected || _username != username || _password != password)
            {
                User = null;
                _username = username;
                _password = password;

                try
                {
                    var user = await _restConnection.MakeRequestAsync<PlexUser, string>(Method.Post,
                        ResponseType.Xml, PlexResources.MyPlexBaseUrl, PlexResources.MyPlexSignIn,
                        _username, _password, headers: PlexHeaders.CreatePlexRequest());

                    User = user.ResponseObject;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception connecting to MyPlex" + ex);
                    User = null;
                }
            }

            await RefreshContainerAsync();
        }

        private Guid _refreshToken;

        public async Task RefreshContainerAsync()
        {
            Guid token;

            lock (_tokenSyncObj)
            {
                token = Guid.NewGuid();
                _refreshToken = token;
            }

            try
            {
                var container = await _restConnection.MakeRequestAsync<MediaContainer, string>(Method.Get,
                    ResponseType.Xml, PlexResources.MyPlexBaseUrl, PlexResources.MyPlexDevices, 
                    headers: PlexHeaders.CreatePlexRequest(User));

                if (container != null && container.ResponseObject != null)
                {
                    bool updated;
                    lock (_deviceSyncObj)
                    {
                        if (token != _refreshToken)
                            return;

                        updated = _devices.UpdateToMatch(container.ResponseObject.Devices, d => d.ClientIdentifier, UpdateDevice);
                        _servers.UpdateToMatch(GetByProvides(container.ResponseObject, "server"), d => d.ClientIdentifier);
                        _players.UpdateToMatch(GetByProvides(container.ResponseObject, "player"), d => d.ClientIdentifier);
                    }

                    if (updated) OnDevicesUpdated();
                }
            }
            catch
            {
                var updated = false;

                lock (_deviceSyncObj)
                {
                    if (token != _refreshToken)
                        return;

                    // lost connection, so clear everything
                    if (_devices.Any())
                    {
                        _devices.Clear();
                        _servers.Clear();
                        _players.Clear();

                        updated = true;
                    }
                }

                if (updated) OnDevicesUpdated();
            }
        }

        private static bool UpdateDevice(Device oldDevice, Device newDevice)
        {
            return oldDevice.UpdateFrom(newDevice);
        }

        private static ICollection<Device> GetByProvides(MediaContainer container, string provides)
        {
            return container.Devices.Where(d => d.Provides.Contains(provides))
                .OrderByDescending(d => d.LastSeenAt).ToList();
        }

        public bool IsConnected { get { return User != null; } }

        public event EventHandler DevicesUpdated;

        private void OnDevicesUpdated()
        {
            var handler = DevicesUpdated;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public async Task<IEnumerable<IPlexServerConnection>> CreateServerConnectionsAsync()
        {
            var serverConnections = new List<IPlexServerConnection>();

            List<Device> servers;

            lock (_deviceSyncObj)
                servers = Servers.ToList();

            foreach (var connection in servers.Select(s => new PlexServerConnection(_restConnection, s, User)))
            {
                await connection.ConnectAsync();
                serverConnections.Add(connection);
            }

            return serverConnections;
        }
    }
}