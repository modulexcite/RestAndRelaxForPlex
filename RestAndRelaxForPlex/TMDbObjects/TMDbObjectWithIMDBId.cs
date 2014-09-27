using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public abstract class TMDbObjectWithIMDBId : TMDbObjectBase
    {
        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }
    }
}
