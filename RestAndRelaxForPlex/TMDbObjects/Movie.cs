using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TmdbObjects
{
    public class Movie : TmdbObjectWithImdbId
    {
        public Credits Credits { get; set; }

        internal static Movie CloneMovie(Movie movie)
        {
            return JsonConvert.DeserializeObject<Movie>(JsonConvert.SerializeObject(movie));
        }
    }
}
