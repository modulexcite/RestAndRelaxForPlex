using System.Collections.Generic;
using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class MovieSearchResults
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; } 
    }
}
