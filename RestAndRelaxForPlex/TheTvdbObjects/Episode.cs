namespace JimBobBennett.RestAndRelaxForPlex.TheTvdbObjects
{
    public class Episode : TvdbObjectBase
    {
        public string EpisodeName { get; set; }
        public int EpisodeNumber { get; set; }
        public int SeasonNumber { get; set; }
    }
}
