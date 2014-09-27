using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JimBobBennett.JimLib.Events;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TMDbObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public interface ITMDbConnection
    {
        void AddMovieToCache(Movie movie);
        void AddPersonToCache(Person person);
        event EventHandler<EventArgs<Movie>> MovieCacheUpdated;
        event EventHandler<EventArgs<Person>> PeopleCacheUpdated;

        Task<Movie> GetMovieAsync(Video video);
        IEnumerable<Person> CachedPeople { get; }
        IEnumerable<Movie> CachedMovies { get; }
    }
}