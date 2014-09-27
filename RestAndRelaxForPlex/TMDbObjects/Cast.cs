using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class Cast : TMDbObjectBase
    {
        [JsonProperty("cast_id")]
        public string CastId { get; set; }

        [JsonProperty("character")]
        public string Character { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profile_path")]
        public string ProfilePath { get; set; }

        public Person Person { get; set; }
    }
}
