using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class Movie : TMDbObjectWithIMDBId
    {
        public Credits Credits { get; set; }

        internal static Movie CloneMovie(Movie movie)
        {
            return JsonConvert.DeserializeObject<Movie>(JsonConvert.SerializeObject(movie));
        }
    }
}
