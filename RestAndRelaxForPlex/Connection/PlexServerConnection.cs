using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.JimLib.Mvvm;
using JimBobBennett.JimLib.Xamarin.Network;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class PlexServerConnection : NotificationObject, IPlexServerConnection
    {
        private readonly IRestConnection _restConnection;
        private Device _device;
        private string _connectionUri;
        private MediaContainer _mediaContainer;

        [NotifyPropertyChangeDependency("Name")]
        public Device Device
        {
            get { return _device; }
            private set
            {
                if (Equals(Device, value)) return;
                _device = value;
                RaisePropertyChanged();
            }
        }

        [NotifyPropertyChangeDependency("Name")]
        public string ConnectionUri
        {
            get { return _connectionUri; }
            private set
            {
                if (Equals(ConnectionUri, value)) return;
                _connectionUri = value;
                RaisePropertyChanged();
            }
        }

        [NotifyPropertyChangeDependency("IsOnLine")]
        [NotifyPropertyChangeDependency("Platform")]
        [NotifyPropertyChangeDependency("MachineIdentifier")]
        [NotifyPropertyChangeDependency("Name")]
        public MediaContainer MediaContainer
        {
            get { return _mediaContainer; }
            private set
            {
                if (Equals(MediaContainer, value)) return;
                _mediaContainer = value;
                RaisePropertyChanged();
            }
        }

        public bool IsOnLine { get { return MediaContainer != null; } }
        public string Platform { get { return MediaContainer != null ? MediaContainer.Platform : string.Empty; } }
        public string MachineIdentifier { get { return MediaContainer != null ? MediaContainer.MachineIdentifier : string.Empty; } }

        public string Name
        {
            get
            {
                if (MediaContainer != null)
                    return MediaContainer.FriendlyName;

                return Device != null ? Device.Name : ConnectionUri;
            }
        }
        
        private readonly ObservableCollectionEx<Video> _nowPlaying = new ObservableCollectionEx<Video>();
        private readonly ObservableCollectionEx<Server> _clients = new ObservableCollectionEx<Server>();

        public ReadOnlyObservableCollection<Video> NowPlaying { get; private set; }
        public ReadOnlyObservableCollection<Server> Clients { get; private set; }
        public PlexUser User { get; set; }

        private PlexServerConnection(IRestConnection restConnection)
        {
            _restConnection = restConnection;
            NowPlaying = new ReadOnlyObservableCollection<Video>(_nowPlaying);
            Clients = new ReadOnlyObservableCollection<Server>(_clients);
        }

        public PlexServerConnection(IRestConnection restConnection, Device device, PlexUser user = null)
            : this(restConnection)
        {
            User = user;
            Device = device;
        }

        public PlexServerConnection(IRestConnection restConnection, string uri, PlexUser user = null)
            : this(restConnection)
        {
            User = user;
            ConnectionUri = TidyUrl(uri);
        }

        public async Task ConnectAsync()
        {
            if (Device != null)
                await MakeConnectionAsync(Device.Connections);
            else
                await TryConnectionAsync(ConnectionUri);

            if (IsOnLine)
                await RefreshSessionAsync();
        }

        private static string TidyUrl(string uri)
        {
            if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                uri = "http://" + uri;

            if (uri.IndexOf(':', 5) == -1)
                uri += ":32400";

            return uri;
        }

        private async Task MakeConnectionAsync(IEnumerable<PlexObjects.Connection> connections)
        {
            foreach (var connection in connections.Where(c => c.Uri != "http://:0"))
            {
                try
                {
                    if (await TryConnectionAsync(connection.Uri))
                    {
                        ConnectionUri = connection.Uri;
                        return;
                    }
                }
// ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    
                }
            }

            ClearMediaContainer();
        }

        private void ClearMediaContainer()
        {
            MediaContainer = null;
            _nowPlaying.Clear();
            _clients.Clear();
        }

        private async Task<RestResponse<T>> MakePlexRequestAsync<T, TData>(string uri, string resource) 
            where T : class, new() where TData : class
        {
            var retVal = await _restConnection.MakeRequestAsync<T, TData>(Method.Get,
                ResponseType.Xml, uri, resource, headers: PlexHeaders.CreatePlexRequest());

            if ((retVal == null || retVal.ResponseObject == null) && User != null)
            {
                retVal = await _restConnection.MakeRequestAsync<T, TData>(Method.Get,
                ResponseType.Xml, uri, resource, headers: PlexHeaders.CreatePlexRequest(User));
            }

            return retVal;
        }

        private async Task<bool> TryConnectionAsync(string uri)
        {
            var mediaContainer = await MakePlexRequestAsync<MediaContainer, string>(uri, "/");

            if (mediaContainer == null) return false;

            if (MediaContainer == null)
            {
                MediaContainer = mediaContainer.ResponseObject;
                await RefreshSessionAsync();
            }
            else
            {
                if (mediaContainer.ResponseObject == null)
                    ClearMediaContainer();
                else
                {
                    if (MediaContainer.UpdateFrom(mediaContainer.ResponseObject))
                        RaisePropertyChanged(() => MediaContainer);

                    await RefreshSessionAsync();
                }
            }

            return true;
        }

        public async Task RefreshAsync()
        {
            var connected = await TryConnectionAsync(ConnectionUri);

            if (connected)
                await RefreshSessionAsync();
            else
                ClearMediaContainer();
        }

        private async Task RefreshSessionAsync()
        {
            IList<Video> videos;
            IList<Server> clients;

            try
            {
                videos = await GetNowPlayingAsync();
            }
            catch
            {
                videos = new List<Video>();
            }

            try
            {
                clients = await GetClientsAsync();
            }
            catch
            {
                clients = new List<Server>();
            }

            foreach (var video in videos)
            {
                var client = clients.FirstOrDefault(c => c.Key == video.Player.Key);
                if (client != null)
                    video.Player.Client = client;
            }

            _clients.ClearAndAddRange(clients);
            _nowPlaying.ClearAndAddRange(videos);
        }

        private async Task<IList<Video>> GetNowPlayingAsync()
        {
            if (ConnectionUri == null)
                return new List<Video>();

            var container = await MakePlexRequestAsync<MediaContainer, string>(ConnectionUri, PlexResources.ServerSessions);

            if (container == null)
                return new List<Video>();

            if (container.ResponseObject == null || container.ResponseObject.Videos == null)
                return new List<Video>();

            return container.ResponseObject.Videos;
        }

        private async Task<IList<Server>> GetClientsAsync()
        {
            var container = await MakePlexRequestAsync<MediaContainer, string>(ConnectionUri,
                PlexResources.ServerClients);

            if (container == null)
                return new List<Server>();

            return container.ResponseObject != null  ? container.ResponseObject.Servers : new ObservableCollectionEx<Server>();
        }

        public async Task PauseVideoAsync(Video video)
        {
            await ChangeClientPlayback(video, PlexResources.ClientPause);
        }

        public async Task PlayVideoAsync(Video video)
        {
            await ChangeClientPlayback(video, PlexResources.ClientPlay);
        }

        public async Task StopVideoAsync(Video video)
        {
            await ChangeClientPlayback(video, PlexResources.ClientStop);
        }

        private async Task ChangeClientPlayback(Video video, string action)
        {
            if (video != null && video.Player != null && video.Player.Client != null)
            {
                var client = video.Player.Client;

                var clientUriBuilder = new UriBuilder
                {
                    Port = client.Port,
                    Host = client.Host,
                    Scheme = "http"
                };

                await MakePlexRequestAsync<Response, string>(clientUriBuilder.Uri.ToString(), action);
            }
        }
    }
}
