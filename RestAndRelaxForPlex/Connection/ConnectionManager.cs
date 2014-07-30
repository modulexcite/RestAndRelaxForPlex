using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TheTvdbObjects;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.JimLib.Events;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.JimLib.Xamarin.Images;
using JimBobBennett.JimLib.Xamarin.Network;
using JimBobBennett.JimLib.Xamarin.Timers;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class ConnectionManager : IConnectionManager
    {
        private const string IpAddress = "239.0.0.250";
        private const int Port = 32414;

        private readonly ITimer _timer;
        private readonly ILocalServerDiscovery _localServerDiscovery;
        private readonly IRestConnection _restConnection;
        private readonly IMyPlexConnection _myPlexConnection;
        private readonly ITheTvdbConnection _theTvdbConnection;
        private readonly IImageHelper _imageHelper;

        private readonly List<PlexServerConnection> _myPlexServerConnections = new List<PlexServerConnection>(); 

        private bool _isPolling;
        private readonly object _syncObject = new object();

        private readonly Dictionary<string, PlexServerConnection> _serverConnections = new Dictionary<string, PlexServerConnection>(); 

        public ConnectionManager(ITimer timer, ILocalServerDiscovery localServerDiscovery,
            IRestConnection restConnection, IMyPlexConnection myPlexConnection, 
            ITheTvdbConnection theTvdbConnection, IImageHelper imageHelper)
        {
            _timer = timer;
            _restConnection = restConnection;
            _myPlexConnection = myPlexConnection;
            _theTvdbConnection = theTvdbConnection;
            _imageHelper = imageHelper;

            _localServerDiscovery = localServerDiscovery;
            _localServerDiscovery.ServerDiscovered += LocalServerDiscoveryOnServerDiscovered;

            NowPlaying = new ReadOnlyObservableCollection<Video>(_nowPlaying);
        }

        private async void LocalServerDiscoveryOnServerDiscovered(object sender, EventArgs<string> eventArgs)
        {
            await CreatePlexServerConnection(eventArgs.Value);
        }

        private async Task<bool> CreatePlexServerConnection(string ipAddress)
        {
            var connection = new PlexServerConnection(_restConnection, ipAddress, _myPlexConnection.User);
            await connection.ConnectAsync();

            var needRebuild = false;

            lock (_syncObject)
            {
                if (!_serverConnections.ContainsKey(connection.MachineIdentifier))
                {
                    _serverConnections.Add(connection.MachineIdentifier, connection);
                    _plexServerConnections.Add(connection);
                    needRebuild = true;
                }
            }

            if (needRebuild)
                await RebuildNowPlaying();

            return connection.IsOnLine;
        }

        public async Task<bool> ConnectToMyPlexAsync(string username, string password)
        {
            var connected = await MakeMyPlexConnection(username, password);
            if (connected)
            {
                foreach (var connection in _plexServerConnections.Where(c => !c.IsOnLine))
                {
                    connection.User = _myPlexConnection.User;
                    await connection.ConnectAsync();
                }
            }

            return connected;
        }

        public async Task<bool> ConnectToServerAsync(string uri)
        {
            return await CreatePlexServerConnection(uri);
        }

        public bool IsConnectedToMyPlex { get { return _myPlexConnection.IsConnected; } }

        private async Task<bool> MakeMyPlexConnection(string username, string password)
        {
            lock (_syncObject)
            {
                if (_myPlexServerConnections.Any())
                {
                    foreach (var myPlexConnection in _myPlexServerConnections)
                    {
                        PlexServerConnection connection;

                        if (_serverConnections.TryGetValue(myPlexConnection.MachineIdentifier, out connection))
                        {
                            _serverConnections.Remove(myPlexConnection.MachineIdentifier);
                            _plexServerConnections.Remove(connection);
                        }
                    }
                }
            }

            await _myPlexConnection.ConnectAsync(username, password);

            if (_myPlexConnection.IsConnected)
            {
                var connections = (await _myPlexConnection.CreateServerConnectionsAsync())
                    .Where(s => s.IsOnLine).Select(p => (PlexServerConnection)p).ToList();

                lock (_syncObject)
                {
                    _myPlexServerConnections.Clear();

                    foreach (var connection in connections.Where(s => !_serverConnections.ContainsKey(s.MachineIdentifier)))
                    {
                        _serverConnections.Add(connection.MachineIdentifier, connection);
                        _plexServerConnections.Add(connection);
                        _myPlexServerConnections.Add(connection);
                    }
                }

                await RebuildNowPlaying();
            }

            return _myPlexConnection.IsConnected;
        }

        public async Task ConnectAsync()
        {
            await _localServerDiscovery.DiscoverLocalServersAsync(IpAddress, Port);
            
            if (!_isPolling)
            {
                _isPolling = true;

                _timer.StartTimer(TimeSpan.FromSeconds(10), async () =>
                    {
                        await _localServerDiscovery.DiscoverLocalServersAsync(IpAddress, Port);
                        return true;
                    });

                _timer.StartTimer(TimeSpan.FromSeconds(1), async () =>
                    {
                        IList<PlexServerConnection> connections;
                        lock (_syncObject)
                            connections = _plexServerConnections.ToList();

                        foreach (var connection in connections)
                        {
                            if (connection.IsOnLine)
                                await connection.RefreshAsync();
                            else
                                await connection.ConnectAsync();
                        }

                        await RebuildNowPlaying();
                        
                        return true;
                    });
            }
        }

        public async Task<Video> RefreshVideo(Video video)
        {
            var connection = (PlexServerConnection)video.PlexServerConnection;
            await connection.RefreshAsync();
            await RebuildNowPlaying();
            return _nowPlaying.FirstOrDefault(v => v.Player.Key == video.Player.Key &&
                v.PlexServerConnection.MachineIdentifier == video.PlexServerConnection.MachineIdentifier);
        }

        private async Task RebuildNowPlaying()
        {
            List<Video> nowPlaying;

            lock (_syncObject)
            {
                nowPlaying = new List<Video>();

                foreach (var connection in _plexServerConnections)
                {
                    foreach (var video in connection.NowPlaying)
                    {
                        video.PlexServerConnection = connection;
                        if (nowPlaying.All(v => v.Player.Key != video.Player.Key && 
                            v.PlexServerConnection.MachineIdentifier != video.PlexServerConnection.MachineIdentifier))
                            nowPlaying.Add(video);
                    }
                }
            }

            foreach (var video in nowPlaying.Where(v => v.Type == VideoType.Episode))
            {
                var series = await _theTvdbConnection.GetSeriesForEpisodeAsync(video);
                if (series != null)
                {
                    if (!video.HasImdbLink && video.HasTvdbLink)
                    {
                        var episode = series.GetEpisode(video.SeasonNumber, video.EpisodeNumber);

                        if (episode != null && !episode.ImdbId.IsNullOrEmpty())
                            video.ImdbId = episode.ImdbId;
                        else if (!series.ImdbId.IsNullOrEmpty())
                            video.ImdbId = series.ImdbId;
                    }

                    MergeRoles(video, series);
                }
            }

            foreach (var video in nowPlaying.Where(v => !v.Thumb.IsNullOrEmpty() &&
                v.ThumbImageSource == null))
            {
                var image = await _imageHelper.GetImageAsync(video.PlexServerConnection.ConnectionUri,
                    video.VideoThumb, headers:PlexHeaders.CreatePlexRequest(video.PlexServerConnection.User),
                    canCache:true);

                if (image != null)
                    video.ThumbImageSource = image.Item2;
            }

            lock (_syncObject)
            {
                _nowPlaying.UpdateToMatch(nowPlaying,
                    v => v.PlexServerConnection.MachineIdentifier + ":" + v.Player.Key, 
                    (v1, v2) => v1.UpdateFrom(v2));
            }
        }

        private static void MergeRoles(Video video, Series series)
        {
            if (series.Actors == null || !series.Actors.Any())
                return;

            var nextId = video.Roles.Any() ? video.Roles.Max(r => r.Id) + 1 : 1;

            foreach (var actor in series.Actors.Where(a => !video.Roles.Any() || 
                video.Roles.All(r => r.RoleName != a.Role && r.Tag != a.Name)))
            {
                video.Roles.Add(new Role
                {
                    Id = nextId,
                    RoleName = actor.Role,
                    Tag = actor.Name,
                    Thumb = actor.Image
                });

                nextId++;
            }
        }

        private readonly ObservableCollectionEx<PlexServerConnection> _plexServerConnections = new ObservableCollectionEx<PlexServerConnection>();
        private readonly ObservableCollectionEx<Video> _nowPlaying = new ObservableCollectionEx<Video>();

        public IEnumerable<Video> NowPlaying { get; private set; } 
    }
}
