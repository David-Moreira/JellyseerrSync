public class SonarrNotificationPayload
{
    public string EventType { get; set; }
    public string InstanceName { get; set; }
    public string ApplicationUrl { get; set; }

    public SonarrSeries Series { get; set; }

    //public List<SonarrEpisode> Episodes { get; set; }

    //public SonarrEpisodeFile EpisodeFile { get; set; }

    public string DeleteReason { get; set; }
}
