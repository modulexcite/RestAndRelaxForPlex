using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class Result : TMDbObjectBase
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }
    }
}
