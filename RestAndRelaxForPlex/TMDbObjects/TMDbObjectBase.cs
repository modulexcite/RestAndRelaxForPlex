using Newtonsoft.Json;

namespace JimBobBennett.RestAndRelaxForPlex.TMDbObjects
{
    public abstract class TMDbObjectBase
    {
        internal const int CurrentVersion = 1;

        protected TMDbObjectBase()
        {
            Version = CurrentVersion;
        }

        public int Version { get; private set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}