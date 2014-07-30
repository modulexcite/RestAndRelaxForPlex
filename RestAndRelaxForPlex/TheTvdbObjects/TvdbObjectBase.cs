using JimBobBennett.JimLib.Xml;

namespace JimBobBennett.RestAndRelaxForPlex.TheTvdbObjects
{
    public class TvdbObjectBase
    {
        internal const int CurrentVersion = 2;

        public TvdbObjectBase()
        {
            Version = CurrentVersion;
        }

        public int Version { get; set; }

        public string Id { get; set; }

        [XmlNameMapping("IMDB_ID")]
        public string ImdbId { get; set; }
    }
}