public class SonarrEpisode
{
    public int Id { get; set; }
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; }
    public string Title { get; set; }
    public string Overview { get; set; }
    public string AirDate { get; set; }
    public DateTime? AirDateUtc { get; set; }
    public int SeriesId { get; set; }
}
