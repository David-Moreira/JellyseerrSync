public class RadarrNotificationPayload
{
    public string EventType { get; set; }
    public string InstanceName { get; set; }
    public string ApplicationUrl { get; set; }

    public RadarrMovie Movie { get; set; }

    public RadarrMovieFile MovieFile { get; set; }

    public string DeleteReason { get; set; }
}
