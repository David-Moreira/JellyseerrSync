using Microsoft.AspNetCore.Mvc;

using static System.Net.WebRequestMethods;

//TODO: Configuration & Secrets
const string JELLYSEERR_APIKEY = "MTY4NjYxNTAzMTI4NjQ0OGY2ODdlLTRlMGYtNGEyMC1iY2EwLWI0OWQ3NDkzOGRhNg==";
const string JELLYFIN_APIKEY = "05092613243f40039715afb2affc8eb3";

const string JELLYFIN_HOST_URL = "http://192.168.1.10:8096/";
const string JELLYSEERR_HOST_URL = "http://192.168.1.11:5055/";

var builder = WebApplication.CreateBuilder( args );

builder.Services.AddHttpClient( "Jellyseerr", ( client ) =>
{
    client.BaseAddress = new Uri( $"{JELLYSEERR_HOST_URL}api/v1/" );
    client.DefaultRequestHeaders.Add( "X-Api-Key", JELLYSEERR_APIKEY );
} );

builder.Services.AddHttpClient( "Jellyfin", ( client ) =>
{
    client.BaseAddress = new Uri( JELLYFIN_HOST_URL );
    client.DefaultRequestHeaders.Add( "Authorization", $"MediaBrowser Token=\"{JELLYFIN_APIKEY}\"" );
} );

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet( "/", async ( context ) => await context.Response.WriteAsync( @"Hello World!

Endpoints:
/radarr/notification
/sonarr/notification

It is of note that any series episode deletion assumes the entire series is deleted. As there seems to be no way to determine if there are episodes left." ) );

app.MapGet( "/sync", async ( [FromServices] IHttpClientFactory httpClientFactory ) =>
{
    Console.WriteLine( "Processing Sync..." );

    var jellyfinClient = httpClientFactory.CreateClient( "Jellyfin" );
    var jellyseerrClient = httpClientFactory.CreateClient( "Jellyseerr" );

    var searchResult = await jellyseerrClient.GetFromJsonAsync<JellyseerrMediaSearchResult>( $"media?take=999&skip=0&filter=available&sort=added" );
    var availableMovies = searchResult.Results.Where( x => x.MediaType.Equals( "movie", StringComparison.InvariantCultureIgnoreCase ) );
    

    var moviesIds = availableMovies
        .Where( x => !string.IsNullOrWhiteSpace( x.JellyfinMediaId ) || !string.IsNullOrWhiteSpace( x.JellyfinMediaId4k ) )
        .Select( x => new
        {
            Id = string.IsNullOrWhiteSpace( x.JellyfinMediaId )
        ? Guid.Parse(x.JellyfinMediaId4k).ToString( "d" )
        : Guid.Parse(x.JellyfinMediaId).ToString("d"),
            MediaId = x.Id,
            TmdbId = x.TmdbId
        } );

    Console.WriteLine( "Available Jellyfin Movies: " + moviesIds.Count() );


    var jellySearchResult = await jellyfinClient.GetFromJsonAsync<JellyfinSearchResult>( $"Items?ids={string.Join(",", moviesIds.Select(x=> x.Id))}&enableTotalRecordCount=false&enableImages=false" );

    var notFoundMovies = moviesIds.Where( x => !jellySearchResult.Items.Any( y => Guid.Parse(y.Id) == Guid.Parse(x.Id) ) );
    var links = notFoundMovies.Select( x => $"{JELLYSEERR_HOST_URL}movie/{x.TmdbId}" );

    Console.WriteLine( "Not Found Movies To Sync: " + notFoundMovies.Count() );

    //foreach (var link in links)
    //{
    //    Console.WriteLine( link );
    //}

    if (notFoundMovies.Count() > 0)
    {
        foreach (var notFoundMovie in notFoundMovies)
        {
            await jellyseerrClient.DeleteAsync( $"media/{notFoundMovie.MediaId}" );
        }
    }

} );

app.MapPost( "/radarr/notification", async ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] RadarrNotificationPayload payload ) =>
{
    Console.WriteLine( "Processing Radarr Notification" );
    Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( payload ) );

    if (payload.EventType.Equals( "MovieFileDelete", StringComparison.InvariantCultureIgnoreCase ) && !payload.DeleteReason.Equals( "upgrade", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing MovieFileDelete" );
        var client = httpClientFactory.CreateClient( "Jellyseerr" );
        var movie = await client.GetFromJsonAsync<JellySeerrMovie>( $"movie/{payload.Movie.TmdbId}" );
        await client.DeleteAsync( $"media/{movie.MediaInfo.Id}" );
    };
} );

app.MapPost( "/sonarr/notification", async ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] SonarrNotificationPayload payload ) =>
{
    Console.WriteLine( "Processing Sonarr Notification" );
    Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( payload ) );

    if (payload.EventType.Equals( "SeriesDelete", StringComparison.InvariantCultureIgnoreCase ) && !payload.DeleteReason.Equals( "upgrade", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing SeriesDelete" );
        var client = httpClientFactory.CreateClient( "Jellyseerr" );

        Console.WriteLine( $"Searching for... {payload.Series.Title}" );
        var searchResult = await client.GetFromJsonAsync<JellyseerrSearchResult>( $"search?query={payload.Series.Title}&page=1&language=en" );

        if (searchResult is not null && searchResult.Results?.Count > 0)
        {
            var foundMedia = searchResult.Results.FirstOrDefault( x => x.MediaInfo?.TvdbId == payload.Series.TvdbId );
            if (foundMedia is not null)
            {
                Console.WriteLine( $"Found existing media for {foundMedia.MediaInfo.TmdbId}" );
                await client.DeleteAsync( $"media/{foundMedia.MediaInfo.Id}" );
            }

        }
    };

    if (payload.EventType.Equals( "EpisodeFileDelete", StringComparison.InvariantCultureIgnoreCase ) && !payload.DeleteReason.Equals( "upgrade", StringComparison.InvariantCultureIgnoreCase ))
    {
        Console.WriteLine( "Processing EpisodeFileDelete" );
        
        var client = httpClientFactory.CreateClient( "Jellyseerr" );

        Console.WriteLine( $"Searching for... {payload.Series.Title}" );
        var searchResult = await client.GetFromJsonAsync<JellyseerrSearchResult>( $"search?query={payload.Series.Title}&page=1&language=en" );

        if (searchResult is not null && searchResult.Results?.Count >0)
        {
            var foundMedia = searchResult.Results.FirstOrDefault( x => x.MediaInfo?.TvdbId == payload.Series.TvdbId );
            if (foundMedia is not null)
            {
                Console.WriteLine( $"Found existing media for {foundMedia.MediaInfo.TmdbId}" );
                await client.DeleteAsync( $"media/{foundMedia.MediaInfo.Id}" );
            }

        }

    };
} );

app.Run();

public class JellyfinSearchResult
{
    public List<JellyfinItem> Items { get; set; }
}
public class JellyfinItem
{
    public string Id { get; set; }
    public string Name { get; set; }

}

public class JellySeerrMovie
{
    public int Id { get; set; }

    public JellyseerrMedia MediaInfo { get; set; }

}

public class JellySeerrTv
{
    public int Id { get; set; }

    public JellyseerrMedia MediaInfo { get; set; }
}

public class JellyseerrMedia
{
    public int Id { get; set; }
    public int TmdbId { get; set; }

    public int TvdbId { get; set; }
    public string MediaType { get; set; }

    public string JellyfinMediaId { get; set; }

    public string JellyfinMediaId4k { get; set; }

}

public class JellyseerrSearchResult
{
    public List<JellyseerrSearchMediaResult> Results { get; set; }
}

public class JellyseerrSearchMediaResult
{
    public int Id { get; set; }
    public JellyseerrMedia MediaInfo { get; set; }
}

public class JellyseerrMediaSearchResult
{
    public List<JellyseerrMedia> Results { get; set; }
}
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