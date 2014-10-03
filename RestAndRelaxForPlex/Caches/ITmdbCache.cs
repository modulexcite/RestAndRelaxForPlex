using System.Threading.Tasks;
using JimBobBennett.RestAndRelaxForPlex.PlexObjects;
using JimBobBennett.RestAndRelaxForPlex.TmdbObjects;

namespace JimBobBennett.RestAndRelaxForPlex.Caches
{
    public interface ITmdbCache
    {
        Task<Movie> GetMovieAsync(Video video);
        Task<TvShow> GetTvShowAsync(Video video);
        Task<Person> GetPersonAsync(string tmdbId);
        string DumpCacheAsJson();
        void LoadCacheFromJson(string json);
    }
}