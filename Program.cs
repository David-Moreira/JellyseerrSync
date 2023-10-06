using Microsoft.AspNetCore.Mvc;

//TODO: Configuration & Secrets
const string APIKEY = "MTY4NjYxNTAzMTI4NjQ0OGY2ODdlLTRlMGYtNGEyMC1iY2EwLWI0OWQ3NDkzOGRhNg==";

var builder = WebApplication.CreateBuilder( args );

builder.Services.AddHttpClient( "JellySeerr", ( client ) =>
{
    client.BaseAddress = new Uri( "http://192.168.1.11:5055/api/v1/" );
    client.DefaultRequestHeaders.Add( "X-Api-Key", APIKEY );
} );

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


app.MapGet( "test", async ( [FromServices] IHttpClientFactory httpClientFactory ) =>
{
    var client = httpClientFactory.CreateClient( "JellySeerr" );
    var movie = await client.GetFromJsonAsync<JellySeerrMovie>( $"movie/{910571}" );

    if (movie?.MediaInfo is not null)
    {
        Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( movie ) );
        await client.DeleteAsync( $"media/{movie.MediaInfo.Id}" );
    }
} );

app.MapPost( "/radarr/notification", async ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] RadarrNotificationPayload payload ) =>
{
    Console.WriteLine( "Processing Radarr Notification" );
    Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( payload ) );

    if (payload.EventType.Equals( "MovieFileDelete", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing MovieFileDelete" );
        var client = httpClientFactory.CreateClient( "JellySeerr" );
        var movie = await client.GetFromJsonAsync<JellySeerrMovie>( $"movie/{payload.Movie.TmdbId}" );
        await client.DeleteAsync( $"media/{movie.MediaInfo.Id}" );
    };
} );

app.MapPost( "/sonarr/notification", async ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] SonarrNotificationPayload payload ) =>
{
    Console.WriteLine( "Processing Sonarr Notification" );
    Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( payload ) );

    if (payload.EventType.Equals( "SeriesDelete", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing SeriesDelete" );
        var client = httpClientFactory.CreateClient( "JellySeerr" );

    };

    if (payload.EventType.Equals( "EpisodeFileDelete", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing EpisodeFileDelete" );
        var client = httpClientFactory.CreateClient( "JellySeerr" );

    };
} );

app.Run();

public class JellySeerrMovie
{
    public int Id { get; set; }

    public JellySeerrMedia MediaInfo { get; set; }

}

public class JellySeerrTv
{
    public int Id { get; set; }

}

public class JellySeerrMedia
{
    public int Id { get; set; }

}

public class SonarrNotificationPayload
{
    public string EventType { get; set; }
    public string InstanceName { get; set; }
    public string ApplicationUrl { get; set; }

    public SonarrSeries Series { get; set; }

    public List<SonarrEpisode> Episodes { get; set; }

    public SonarrEpisodeFile EpisodeFile { get; set; }

    public string DeleteReason { get; set; }
}

public class SonarrSeries
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string TitleSlug { get; set; }
    public string Path { get; set; }
    public int TvdbId { get; set; }
    public int TvMazeId { get; set; }
    public string ImdbId { get; set; }
    //public SeriesTypes Type { get; set; }
    public int Year { get; set; }
}

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

public class SonarrEpisodeFile
{

    public int Id { get; set; }
    public string RelativePath { get; set; }
    public string Path { get; set; }
    public string Quality { get; set; }
    public int QualityVersion { get; set; }
    public string ReleaseGroup { get; set; }
    public string SceneName { get; set; }
    public long Size { get; set; }
    public DateTime DateAdded { get; set; }
    //public WebhookEpisodeFileMediaInfo MediaInfo { get; set; }
}

public class RadarrNotificationPayload
{
    public string EventType { get; set; }
    public string InstanceName { get; set; }
    public string ApplicationUrl { get; set; }

    public RadarrMovie Movie { get; set; }

    public RadarrMovieFile MovieFile { get; set; }

    public string DeleteReason { get; set; }
}


public class RadarrMovie
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int Year { get; set; }
    public string FilePath { get; set; }
    public string ReleaseDate { get; set; }
    public string FolderPath { get; set; }
    public int TmdbId { get; set; }
    public string ImdbId { get; set; }
    public string Overview { get; set; }

}

public class RadarrMovieFile
{
    public int Id { get; set; }
    public string RelativePath { get; set; }
    public string Path { get; set; }
    public string Quality { get; set; }
    public int QualityVersion { get; set; }
    public string ReleaseGroup { get; set; }
    public string SceneName { get; set; }
    public string IndexerFlags { get; set; }
    public long Size { get; set; }
    public DateTime DateAdded { get; set; }
    public RadarrMovieFileMediaInfo MediaInfo { get; set; }
}

public class RadarrMovieFileMediaInfo
{
    public decimal AudioChannels { get; set; }
    public string AudioCodec { get; set; }
    public List<string> AudioLanguages { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public List<string> Subtitles { get; set; }
    public string VideoCodec { get; set; }
    public string VideoDynamicRange { get; set; }
    public string VideoDynamicRangeType { get; set; }
}