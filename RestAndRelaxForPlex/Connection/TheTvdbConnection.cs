using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TheTvdbObjects;
using JimBobBennett.JimLib.Events;
using JimBobBennett.JimLib.Xamarin.Network;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class TheTvdbConnection : ITheTvdbConnection
    {
        private readonly Dictionary<string, Series> _cachedSeries = new Dictionary<string, Series>();
        public IEnumerable<Series> CachedSeries
        {
            get { return _cachedSeries.Values.Select(Series.CloneSeries).ToList(); }
        }
 
        private readonly IRestConnection _restConnection;

        public TheTvdbConnection(IRestConnection restConnection)
        {
            _restConnection = restConnection;
        }

        public void AddSeriesToCache(Series series)
        {
            if (series.Version != TvdbObjectBase.CurrentVersion) return;
            if (series.Actors == null) return;

            _cachedSeries[series.Id] = series;
            WeakEventManager.GetWeakEventManager(this).RaiseEvent(this, new EventArgs<Series>(series), "CacheUpdated");
        }

        public event EventHandler<EventArgs<Series>> CacheUpdated
        {
            add { WeakEventManager.GetWeakEventManager(this).AddEventHandler("CacheUpdated", value); }
            remove { WeakEventManager.GetWeakEventManager(this).RemoveEventHandler("CacheUpdated", value); }
        }

        public async Task<Series> GetSeriesForEpisodeAsync(Video video)
        {
            if (!video.HasTvdbLink) return null;

            Series series;
            if (_cachedSeries.TryGetValue(video.TvdbId, out series) && SeriesIsUpToDateForEpisode(video, series))
                return series;

            var data = await _restConnection.MakeRequestAsync<Data, object>(Method.Get, ResponseType.Xml,
                PlexResources.TheTvdbBaseUrl, string.Format(PlexResources.TheTvdbSeries, video.TvdbId),
                timeout: 30000);

            if (data == null || data.ResponseObject == null) return null;

            series = data.ResponseObject.Series;
            series.Episodes = data.ResponseObject.Episodes;

            await GetActorsForSeriesAsync(series);
            AddSeriesToCache(series);

            return series;
        }

        private async Task GetActorsForSeriesAsync(Series series)
        {
            var data = await _restConnection.MakeRequestAsync<SeriesActors, object>(Method.Get, ResponseType.Xml,
                PlexResources.TheTvdbBaseUrl, string.Format(PlexResources.TheTvdbSeriesActors, series.Id),
                timeout: 30000);

            if (data != null && data.ResponseObject != null)
            {
                var actors = data.ResponseObject;
                foreach (var actor in actors.Actors.Where(a => !a.Image.StartsWith(PlexResources.TheTvdbActorImageRoot)))
                    actor.Image = PlexResources.TheTvdbActorImageRoot + actor.Image;
                
                series.Actors = data.ResponseObject.Actors;
            }
        }

        private static bool SeriesIsUpToDateForEpisode(Video video, Series series)
        {
            return series.GetEpisode(video.SeasonNumber, video.EpisodeNumber) != null;
        }
    }
}
