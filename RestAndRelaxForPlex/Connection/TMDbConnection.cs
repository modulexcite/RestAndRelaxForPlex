using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.JimLib.Events;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.JimLib.Xamarin.Network;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TMDbObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class TMDbConnection : ITMDbConnection
    {
        private const int FailedLookupExpiryMinutes = 30;
        private readonly Dictionary<string, Person> _cachedPeople = new Dictionary<string, Person>();
        private readonly Dictionary<string, Movie> _cachedMovies = new Dictionary<string, Movie>();
        private readonly Dictionary<string, Movie> _cachedMoviesByImdb = new Dictionary<string, Movie>();

        public IEnumerable<Person> CachedPeople
        {
            get { return _cachedPeople.Values.Select(Person.ClonePerson).ToList(); }
        }

        public IEnumerable<Movie> CachedMovies
        {
            get { return _cachedMovies.Values.Select(Movie.CloneMovie).ToList(); }
        }

        private readonly Dictionary<string, DateTime> _failedMovies = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> _failedMoviesByImdb = new Dictionary<string, DateTime>(); 
        
        private readonly IRestConnection _restConnection;
        private readonly string _apiKey;

        public TMDbConnection(IRestConnection restConnection, string apiKey)
        {
            _restConnection = restConnection;
            _apiKey = apiKey;
        }
        
        public void AddMovieToCache(Movie movie)
        {
            if (movie.Version != TMDbObjectBase.CurrentVersion) return;

            _cachedMovies[movie.Id] = movie;
            _cachedMoviesByImdb[movie.ImdbId] = movie;
            WeakEventManager.GetWeakEventManager(this).RaiseEvent(this, new EventArgs<Movie>(movie), "MovieCacheUpdated");
        }

        public void AddPersonToCache(Person person)
        {
            if (person.Version != TMDbObjectBase.CurrentVersion) return;

            _cachedPeople[person.Id] = person;
            WeakEventManager.GetWeakEventManager(this).RaiseEvent(this, new EventArgs<Person>(person), "PeopleCacheUpdated");
        }

        public event EventHandler<EventArgs<Movie>> MovieCacheUpdated
        {
            add { WeakEventManager.GetWeakEventManager(this).AddEventHandler("MovieCacheUpdated", value); }
            remove { WeakEventManager.GetWeakEventManager(this).RemoveEventHandler("MovieCacheUpdated", value); }
        }

        public event EventHandler<EventArgs<Person>> PeopleCacheUpdated
        {
            add { WeakEventManager.GetWeakEventManager(this).AddEventHandler("PeopleCacheUpdated", value); }
            remove { WeakEventManager.GetWeakEventManager(this).RemoveEventHandler("PeopleCacheUpdated", value); }
        }
 
        public async Task<Movie> GetMovieAsync(Video video)
        {
            if (!video.HasTmdbLink) 
                return await SearchForMovie(video);

            return await LoadMovieAsync(video.TmdbId);
        }

        private async Task<Movie> LoadMovieAsync(string tmdbId)
        {
            DateTime failedDate;
            if (_failedMovies.TryGetValue(tmdbId, out failedDate))
            {
                if (DateTime.Now.Subtract(failedDate).TotalMinutes > FailedLookupExpiryMinutes)
                    _failedMovies.Remove(tmdbId);
                else
                    return null;
            }

            Movie movie;
            if (_cachedMovies.TryGetValue(tmdbId, out movie))
                return movie;

            var response = await _restConnection.MakeRequestAsync<Movie, object>(Method.Get, ResponseType.Json,
                PlexResources.TMDbBaseUrl, string.Format(PlexResources.TMDbMovie, tmdbId, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null)
            {
                _failedMovies[tmdbId] = DateTime.Now;
                return null;
            }

            movie = response.ResponseObject;

            if (await LoadCreditsForMovie(movie))
                AddMovieToCache(movie);
            else
                _failedMovies[tmdbId] = DateTime.Now;

            return movie;
        }

        private async Task<Movie> SearchForMovie(Video video)
        {
            if (!video.HasImdbLink)
                return null;

            DateTime failedDate;
            if (_failedMoviesByImdb.TryGetValue(video.ImdbId, out failedDate))
            {
                if (DateTime.Now.Subtract(failedDate).TotalMinutes > FailedLookupExpiryMinutes)
                    _failedMoviesByImdb.Remove(video.ImdbId);
                else
                    return null;
            }

            Movie movie;
            if (_cachedMoviesByImdb.TryGetValue(video.ImdbId, out movie))
                return movie;

            var response = await _restConnection.MakeRequestAsync<MovieSearchResults, object>(Method.Get, ResponseType.Json,
                PlexResources.TMDbBaseUrl, string.Format(PlexResources.TMDbSearchMovie, video.Title, video.Year, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null || !response.ResponseObject.Results.Any())
            {
                _failedMoviesByImdb[video.ImdbId] = DateTime.Now;
                return null;
            }

            var results = response.ResponseObject.Results.Where(r => r.Title == video.Title).ToList();
            if (results.Count() > 1)
                results = results.Where(r => r.ReleaseDate == video.OriginallyAvailableAt).ToList();

            if (results.Count() != 1)
            {
                _failedMoviesByImdb[video.ImdbId] = DateTime.Now;
                return null;
            }

            video.TmdbId = results.Single().Id;

            if (video.TmdbId.IsNullOrEmpty())
            {
                _failedMoviesByImdb[video.ImdbId] = DateTime.Now;
                return null;
            }

            return await LoadMovieAsync(video.TmdbId);
        }

        private async Task<bool> LoadCreditsForMovie(Movie movie)
        {
            var response = await _restConnection.MakeRequestAsync<Credits, object>(Method.Get,
                ResponseType.Json, PlexResources.TMDbBaseUrl,
                string.Format(PlexResources.TMDbCredits, movie.Id, _apiKey), timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return false;

            var credits = response.ResponseObject;
            movie.Credits = credits;

            var retVal = true;

            foreach (var cast in credits.Cast.Where(c => !c.Id.IsNullOrEmpty()))
            {
                cast.Person = await GetPerson(cast.Id);
                if (cast.Person == null)
                    retVal = false;
            }

            return retVal;
        }

        private async Task<Person> GetPerson(string id)
        {
            Person person;
            if (_cachedPeople.TryGetValue(id, out person))
                return person;

            var response = await _restConnection.MakeRequestAsync<Person, object>(Method.Get,
                ResponseType.Json, PlexResources.TMDbBaseUrl,
                string.Format(PlexResources.TMDbPerson, id, _apiKey), timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return null;

            person = response.ResponseObject;

            AddPersonToCache(person);

            return person;
        }
    }
}
