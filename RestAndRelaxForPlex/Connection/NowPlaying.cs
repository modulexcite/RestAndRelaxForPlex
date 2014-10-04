using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class NowPlaying : INowPlaying
    {
        public ReadOnlyObservableCollection<Video> VideosNowPlaying { get; private set; }

        public Video GetVideo(string connectionMachineId, string playerId)
        {
            Dictionary<string, Video> byPlayer;
            if (_nowPlayingByServerAndPlayer.TryGetValue(connectionMachineId, out byPlayer))
            {
                Video video;
                if (byPlayer.TryGetValue(playerId, out video))
                    return video;
            }

            return null;
        }

        private readonly ObservableCollectionEx<Video> _videosNowPlaying= new ObservableCollectionEx<Video>();

        private readonly Dictionary<string, Dictionary<string, Video>> _nowPlayingByServerAndPlayer = new Dictionary<string, Dictionary<string, Video>>();

        public NowPlaying()
        {
            VideosNowPlaying = new ReadOnlyObservableCollection<Video>(_videosNowPlaying);
        }

        internal void UpdateNowPlaying(ICollection<Video> nowPlaying)
        {
            var existing = new List<Video>(_videosNowPlaying);

            // first delete all that are not playing
            foreach (var video in existing.ToList())
            {
                if (!nowPlaying.Any(v => VideosMatchByServerAndPlayer(v, video)))
                {
                    var connectionKey = video.PlexServerConnection.MachineIdentifier;
                    var playerKey = video.Player.MachineIdentifier;

                    existing.Remove(video);

                    Dictionary<string, Video> byPlayer;
                    if (_nowPlayingByServerAndPlayer.TryGetValue(connectionKey, out byPlayer))
                    {
                        if (byPlayer.Remove(playerKey) && !byPlayer.Any())
                            _nowPlayingByServerAndPlayer.Remove(connectionKey);
                    }
                }
            }

            // now add all new ones or update
            foreach (var video in nowPlaying)
            {
                var connectionKey = video.PlexServerConnection.MachineIdentifier;
                var playerKey = video.Player.MachineIdentifier;

                Dictionary<string, Video> byPlayer;
                if (!_nowPlayingByServerAndPlayer.TryGetValue(connectionKey, out byPlayer))
                {
                    byPlayer = new Dictionary<string, Video>();
                    _nowPlayingByServerAndPlayer.Add(connectionKey, byPlayer);
                }

                Video oldVideo;
                if (!byPlayer.TryGetValue(playerKey, out oldVideo))
                {
                    byPlayer.Add(playerKey, video);
                    existing.Add(video);
                }
                else
                {
                    var matches = video.Guid == oldVideo.Guid;

                    if (matches)
                    {
                        oldVideo.ViewOffset = video.ViewOffset;
                        oldVideo.PlayerState = video.PlayerState;
                    }
                    else
                        byPlayer[playerKey] = video;
                }
            }

            existing.Sort(Comparer<Video>.Create((v1, v2) => System.String.Compare(v1.Title, v2.Title, System.StringComparison.Ordinal)));

            var needClearAndAdd = existing.Count != _videosNowPlaying.Count;
            if (!needClearAndAdd)
            {
                for (var i = 0; i < existing.Count && !needClearAndAdd; i++)
                {
                    if (existing[i] != _videosNowPlaying[i])
                        needClearAndAdd = true;
                }
            }

            if (needClearAndAdd)
                _videosNowPlaying.ClearAndAddRange(existing);
        }

        private static bool VideosMatchByServerAndPlayer(Video v, Video video)
        {
            return v.PlexServerConnection.MachineIdentifier == video.PlexServerConnection.MachineIdentifier && 
                   v.Player.MachineIdentifier == video.Player.MachineIdentifier;
        }
    }
}
