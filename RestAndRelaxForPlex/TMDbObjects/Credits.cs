using System.Collections.Generic;
using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public class Credits : TMDbObjectBase
    {
        [JsonProperty("cast")]
        public List<Cast> Cast { get; set; } 
    }
}
