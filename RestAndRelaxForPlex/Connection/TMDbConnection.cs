﻿using System.Linq;
using System.Threading.Tasks;
using JimBobBennett.JimLib.Extensions;
using JimBobBennett.JimLib.Xamarin.Network;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TmdbObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Connection
{
    public class TmdbConnection : ITmdbConnection
    {
        private readonly IRestConnection _restConnection;
        private readonly string _apiKey;

        public TmdbConnection(IRestConnection restConnection, string apiKey)
        {
            _restConnection = restConnection;
            _apiKey = apiKey;
        }

        public async Task<Movie> GetMovieAsync(string tmdbId)
        {
            var movie = await LoadMovieAsync(tmdbId);
            return (movie == null || !await LoadCreditsForMovieAsync(movie)) ? null : movie;
        }

        private async Task<Movie> LoadMovieAsync(string tmdbId, int timeout= 30000)
        {
            var response = await _restConnection.MakeRequestAsync<Movie, object>(Method.Get, ResponseType.Json,
                PlexResources.TmdbBaseUrl, string.Format(PlexResources.TmdbMovie, tmdbId, _apiKey),
                timeout: timeout);

            return response == null ? null : response.ResponseObject;
        }

        public async Task<TvShow> GetTvShowAsync(string tmdbId, int seasonNumber, int episodeNumber)
        {
            var tvShow = await LoadTvShowAsync(tmdbId);

            return (tvShow == null ||
                    !await LoadCreditsForTvShowAsync(tvShow, seasonNumber, episodeNumber) ||
                    !await LoadExternalIdsForTvShowAsync(tvShow, seasonNumber, episodeNumber)) ? null : tvShow;
        }

        private async Task<TvShow> LoadTvShowAsync(string tmdbId, int timeout = 30000)
        {
            var response = await _restConnection.MakeRequestAsync<TvShow, object>(Method.Get, ResponseType.Json,
                PlexResources.TmdbBaseUrl, string.Format(PlexResources.TmdbTvShow, tmdbId, _apiKey),
                timeout: timeout);

            return response == null ? null : response.ResponseObject;
        }

        public async Task<Movie> SearchForMovieAsync(string title, int year, ExternalIds knownIds)
        {
            var response = await _restConnection.MakeRequestAsync<SearchResults, object>(Method.Get, ResponseType.Json,
                PlexResources.TmdbBaseUrl, string.Format(PlexResources.TmdbSearchMovie, title, year, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null || !response.ResponseObject.Results.Any())
                return null;

            var results = response.ResponseObject.Results.Where(r => r.Title == title).ToList();
            if (results.Count() == 1)
                return await GetMovieAsync(results.Single().Id);

            if (knownIds.ImdbId.IsNullOrEmpty()) return null;

            // try matching ids
            foreach (var result in results)
            {
                var movie = await LoadMovieAsync(result.Id, 10000);
                if (movie.ImdbId == knownIds.ImdbId)
                {
                    if (!await LoadCreditsForMovieAsync(movie)) return null;
                    return movie;
                }
            }

            return null;
        }

        public async Task<TvShow> SearchForTvShowAsync(string title, int year, ExternalIds knownSeriesIds, 
            int seasonNumber, int episodeNumber)
        {
            // handle cases like Castle (called Castle(2009) on TVDB)
            var yearPart = " (" + year + ")";
            if (title.EndsWith(yearPart))
                title = title.Replace(yearPart, "");

            var response = await _restConnection.MakeRequestAsync<SearchResults, object>(Method.Get, ResponseType.Json,
                PlexResources.TmdbBaseUrl, string.Format(PlexResources.TmdbSearchTvShow, title, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null || !response.ResponseObject.Results.Any())
                return null;

            var results = response.ResponseObject.Results.Where(r => r.Name == title).ToList();
            if (results.Count() == 1)
                return await GetTvShowAsync(results.Single().Id, seasonNumber, episodeNumber);

            results = results.Where(r => r.FirstAirDate.Contains(year.ToString())).ToList();
            if (results.Count() == 1)
                return await GetTvShowAsync(results.Single().Id, seasonNumber, episodeNumber);

            if (knownSeriesIds.ImdbId.IsNullOrEmpty() && knownSeriesIds.TvdbId.IsNullOrEmpty()) return null;

            // try matching ids
            foreach (var result in results)
            {
                var tvShow = await LoadTvShowAsync(result.Id, 10000);
                if (tvShow.ExternalExternalIds.ImdbId == knownSeriesIds.ImdbId ||
                    tvShow.ExternalExternalIds.TvdbId == knownSeriesIds.TvdbId)
                    return (!await LoadCreditsForTvShowAsync(tvShow, seasonNumber, episodeNumber) ||
                            !await LoadExternalIdsForTvShowAsync(tvShow, seasonNumber, episodeNumber)) ? null : tvShow;
            }

            return null;
        }

        private async Task<bool> LoadCreditsForMovieAsync(Movie movie)
        {
            var response = await _restConnection.MakeRequestAsync<Credits, object>(Method.Get,
                ResponseType.Json, PlexResources.TmdbBaseUrl,
                string.Format(PlexResources.TmdbCredits, movie.Id, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return false;

            movie.Credits = response.ResponseObject;

            PopulateImagePaths(movie.Credits);

            return true;
        }

        private static void PopulateImagePaths(Credits credits)
        {
            if (credits != null)
            {
                if (credits.Cast != null)
                {
                    foreach (var cast in credits.Cast.Where(c => !c.ProfilePath.IsNullOrEmpty()))
                        cast.ProfilePath = PlexResources.TmdbActorImageRoot + cast.ProfilePath;
                }

                if (credits.GuestStars != null)
                {
                    foreach (var cast in credits.GuestStars.Where(c => !c.ProfilePath.IsNullOrEmpty()))
                        cast.ProfilePath = PlexResources.TmdbActorImageRoot + cast.ProfilePath;
                }
            }
        }

        private async Task<bool> LoadCreditsForTvShowAsync(TvShow tvShow, int seasonNumber, int episodeNumber)
        {
            var response = await _restConnection.MakeRequestAsync<Credits, object>(Method.Get,
                ResponseType.Json, PlexResources.TmdbBaseUrl,
                string.Format(PlexResources.TmdbTvShowCredits, tvShow.Id, seasonNumber, episodeNumber, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return false;

            tvShow.Credits = response.ResponseObject;

            PopulateImagePaths(tvShow.Credits);
            return true;
        }
        
        private async Task<bool> LoadExternalIdsForTvShowAsync(TvShow tvShow, int seasonNumber, int episodeNumber)
        {
            var response = await _restConnection.MakeRequestAsync<TmdbExternalIds, object>(Method.Get,
                ResponseType.Json, PlexResources.TmdbBaseUrl,
                string.Format(PlexResources.TmdbTvShowExternalIds, tvShow.Id, seasonNumber, episodeNumber, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return false;

            tvShow.EpisodeExternalIds = response.ResponseObject;

            response = await _restConnection.MakeRequestAsync<TmdbExternalIds, object>(Method.Get,
                ResponseType.Json, PlexResources.TmdbBaseUrl,
                string.Format(PlexResources.TmdbTvShowSeriesExternalIds, tvShow.Id, _apiKey),
                timeout: 30000);

            if (response == null || response.ResponseObject == null)
                return false;

            tvShow.ExternalExternalIds = response.ResponseObject;

            return true;
        }

        public async Task<Person> GetPersonAsync(string tmdbId)
        {
            var response = await _restConnection.MakeRequestAsync<Person, object>(Method.Get, ResponseType.Json,
                PlexResources.TmdbBaseUrl, string.Format(PlexResources.TmdbPerson, tmdbId, _apiKey),
                timeout: 30000);

            return response == null ? null : response.ResponseObject;
        }
    }
}
