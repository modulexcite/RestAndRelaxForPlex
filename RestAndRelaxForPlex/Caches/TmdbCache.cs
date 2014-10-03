using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using JimBobBennett.JimLib.Collections;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.RestAndRelaxForPlex.Connection;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TmdbObjects;
using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.Caches
{
    public class TmdbCache : ITmdbCache
    {
        private readonly ITmdbConnection _tmdbConnection;

        private readonly CacheWithFail<Tuple<string, int, string>, Movie> _movieCacheByTitle = new CacheWithFail<Tuple<string, int, string>, Movie>();
        private readonly CacheWithFail<string, Movie> _movieCache = new CacheWithFail<string, Movie>();

        private readonly CacheWithFail<Tuple<string, int, string, string>, TvShow> _tvShowCacheByTitle = new CacheWithFail<Tuple<string, int, string, string>, TvShow>();
        private readonly CacheWithFail<string, TvShow> _tvShowCache = new CacheWithFail<string, TvShow>();

        private readonly CacheWithFail<string, Person> _peopleCache = new CacheWithFail<string, Person>();

        public TmdbCache(ITmdbConnection tmdbConnection)
        {
            _tmdbConnection = tmdbConnection;
        }

        public async Task<Movie> GetMovieAsync(Video video)
        {
            Movie movie;

            if (!video.ExternalIds.TmdbId.IsNullOrEmpty())
            {

                movie = await _movieCache.GetOrAddAsync(video.ExternalIds.TmdbId,
                    async s => await _tmdbConnection.GetMovieAsync(s));

                if (movie != null)
                {
                    _movieCacheByTitle.Add(Tuple.Create(video.Title, video.Year, movie.ImdbId), movie);
                    return movie;
                }
            }

            if (video.ExternalIds.ImdbId.IsNullOrEmpty())
            {
                movie = await _tmdbConnection.SearchForMovieAsync(video.Title, video.Year, video.ExternalIds);

                if (movie != null)
                {
                    _movieCache.Add(movie.Id, movie);
                    _movieCacheByTitle.Add(Tuple.Create(video.Title, video.Year, movie.ImdbId), movie);
                }

                return movie;
            }

            var key = Tuple.Create(video.Title, video.Year, video.ExternalIds.ImdbId);
            movie = await _movieCacheByTitle.GetOrAddAsync(key, async s => await _tmdbConnection.SearchForMovieAsync(video.Title, video.Year, video.ExternalIds));

            if (movie != null)
                _movieCache.Add(movie.Id, movie);

            return movie;
        }

        public async Task<TvShow> GetTvShowAsync(Video video)
        {
            TvShow show;

            if (!video.ExternalIds.TmdbId.IsNullOrEmpty())
            {
                show = await _tvShowCache.GetOrAddAsync(video.ExternalIds.TmdbId,
                    async s => await _tmdbConnection.GetTvShowAsync(s, video.SeasonNumber, video.EpisodeNumber));

                if (show != null)
                {
                    _tvShowCacheByTitle.Add(Tuple.Create(video.Show, video.Year, show.ExternalExternalIds.ImdbId,
                        show.ExternalExternalIds.TvdbId),
                        show);

                    return show;
                }
            }

            if (video.ExternalIds.ImdbId.IsNullOrEmpty() && video.ExternalIds.TvdbId.IsNullOrEmpty())
            {
                show = await _tmdbConnection.SearchForTvShowAsync(video.Show, video.Year, video.ExternalIds,
                    video.SeasonNumber, video.EpisodeNumber);

                if (show != null)
                {
                    _tvShowCache.Add(show.Id, show);
                    _tvShowCacheByTitle.Add(Tuple.Create(video.Show, video.Year, show.ExternalExternalIds.ImdbId,
                        show.ExternalExternalIds.TvdbId), show);
                }

                return show;
            }

            var key = Tuple.Create(video.Show, video.Year, video.ExternalIds.ImdbId, video.ExternalIds.TvdbId);
            show = await _tvShowCacheByTitle.GetOrAddAsync(key,
                async s => await _tmdbConnection.SearchForTvShowAsync(video.Show, video.Year, video.ExternalIds,
                    video.SeasonNumber, video.EpisodeNumber));

            if (show != null)
                _tvShowCache.Add(show.Id, show);

            return show;
        }

        public async Task<Person> GetPersonAsync(string tmdbId)
        {
            return await _peopleCache.GetOrAddAsync(tmdbId, async s => await _tmdbConnection.GetPersonAsync(tmdbId));
        }

        public string DumpCacheAsJson()
        {
            var caches = new Dictionary<string, string>
            {
                {"_movieCache", _movieCache.DumpCacheAsJson()}, 
                {"_movieCacheByTitle", _movieCacheByTitle.DumpCacheAsJson()}, 
                {"_peopleCache", _peopleCache.DumpCacheAsJson()}, 
                {"_tvShowCache", _tvShowCache.DumpCacheAsJson()},
                {"_tvShowCacheByTitle", _tvShowCacheByTitle.DumpCacheAsJson()}
            };

            return JsonConvert.SerializeObject(caches);
        }

        public void LoadCacheFromJson(string json)
        {
            if (json.IsNullOrEmpty()) return;

            try
            {
                var caches = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                string output;
                if (caches.TryGetValue("_movieCache", out output))
                    _movieCache.LoadCacheFromJson(output);

                if (caches.TryGetValue("_movieCacheByTitle", out output))
                    _movieCacheByTitle.LoadCacheFromJson(output);

                if (caches.TryGetValue("_peopleCache", out output))
                    _peopleCache.LoadCacheFromJson(output);

                if (caches.TryGetValue("_tvShowCache", out output))
                    _tvShowCache.LoadCacheFromJson(output);

                if (caches.TryGetValue("_tvShowCacheByTitle", out output))
                    _tvShowCacheByTitle.LoadCacheFromJson(output);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to deserialize caches: " + ex.Message);
            }
        }
    }
}
