using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TheTvdbObjects;
using JimBobBennett.JimLib.Events;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public interface ITheTvdbConnection
    {
        Task<Series> GetSeriesForEpisodeAsync(Video video);
        IEnumerable<Series> CachedSeries { get; }
        void AddSeriesToCache(Series series);

        event EventHandler<EventArgs<Series>> CacheUpdated;
    }
}