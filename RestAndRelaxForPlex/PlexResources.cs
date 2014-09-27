namespace JimBobBennett.RestAndRelaxForPlex
{
    internal static class PlexResources
    {
        public const string MyPlexSignIn = "users/sign_in.xml";
        public const string MyPlexDevices = "devices.xml";

        public const string MyPlexBaseUrl = "https://plex.tv";

        public const string ServerSessions = "status/sessions";
        public const string ServerClients = "clients";

        public const string ClientPause = "player/playback/pause";
        public const string ClientPlay = "player/playback/play";
        public const string ClientStop = "player/playback/stop";

        public const string TheTvdbBaseUrl = "http://thetvdb.com";
        public const string TheTvdbSeries = "data/series/{0}/all";
        public const string TheTvdbSeriesActors = "data/series/{0}/actors.xml";
        public const string TheTvdbActorImageRoot = "http://thetvdb.com/banners/";
        public const string TheTvdbUrl = "http://www.thetvdb.com/?tab=series&id={0}";

        public const string TMDbBaseUrl = "https://api.themoviedb.org";
        public const string TMDbActorImageRoot = "http://image.tmdb.org/t/p/original";
        public const string TMDbMovie = "3/movie/{0}?api_key={1}";
        public const string TMDbPerson = "3/person/{0}?api_key={1}";
        public const string TMDbCredits = "3/movie/{0}/credits?api_key={1}";
        public const string TMDbSearchMovie = "/3/search/movie?query={0}&include_adult=true&year={1}&api_key={2}";
        public const string TMDbMovieUrl = "https://www.themoviedb.org/movie/{0}";

        public const string ImdbNameUrl = "http://www.imdb.com/name/{0}/";
        public const string ImdbNameSchemeUrl = "imdb:///name/{0}/";
        public const string ImdbTitleUrl = "http://www.imdb.com/title/{0}";
        public const string ImdbTitleSchemeUrl = "imdb:///title/{0}/";
    }
}
