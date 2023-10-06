using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder( args );

var configuration = new ConfigurationBuilder()
    .SetBasePath( Directory.GetCurrentDirectory() )
    .AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
    .AddEnvironmentVariables()
    .AddUserSecrets( typeof( Program ).Assembly )
    .Build();

var JELLYSEERR_APIKEY = configuration.GetSection( "JELLYSEERR_APIKEY" ).Value;
var JELLYFIN_APIKEY = configuration.GetSection( "JELLYFIN_APIKEY" ).Value;
var JELLYSEERR_HOST_URL = configuration.GetSection( "JELLYSEERR_HOST_URL" ).Value;
var JELLYFIN_HOST_URL = configuration.GetSection( "JELLYFIN_HOST_URL" ).Value;

ArgumentNullException.ThrowIfNullOrEmpty( JELLYSEERR_APIKEY );
ArgumentNullException.ThrowIfNullOrEmpty( JELLYSEERR_HOST_URL );
ArgumentNullException.ThrowIfNullOrEmpty( JELLYFIN_HOST_URL );
ArgumentNullException.ThrowIfNullOrEmpty( JELLYFIN_APIKEY );

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

app.UseHttpsRedirection();

app.MapGet( "/", async ( context ) => await context.Response.WriteAsync( @"
Endpoints:
/radarr/notification
/sonarr/notification

It is of note that any series episode deletion assumes the entire series is deleted. As there seems to be no way to determine if there are episodes left." ) );

app.MapGet( "/syncdeleted/movies", ( [FromServices] IHttpClientFactory httpClientFactory )
    => SyncDeletedMovies( httpClientFactory ) );

app.MapPost( "/radarr/notification", ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] RadarrNotificationPayload payload )
    => ProcessRadarrNotification( httpClientFactory, payload ) );

app.MapPost( "/sonarr/notification", ( [FromServices] IHttpClientFactory httpClientFactory, [FromBody] SonarrNotificationPayload payload )
    => ProcessSonarrNotification( httpClientFactory, payload ) );


app.Run();


static async Task ProcessRadarrNotification( IHttpClientFactory httpClientFactory, RadarrNotificationPayload payload )
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
}

static async Task ProcessSonarrNotification( IHttpClientFactory httpClientFactory, SonarrNotificationPayload payload )
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
}

static async Task SyncDeletedMovies( IHttpClientFactory httpClientFactory )
{
    Console.WriteLine( "Processing Deleted Movies Sync..." );

    var jellyfinClient = httpClientFactory.CreateClient( "Jellyfin" );
    var jellyseerrClient = httpClientFactory.CreateClient( "Jellyseerr" );

    var searchResult = await jellyseerrClient.GetFromJsonAsync<JellyseerrMediaSearchResult>( $"media?take=999&skip=0&filter=available&sort=added" );
    var availableMovies = searchResult.Results.Where( x => x.MediaType.Equals( "movie", StringComparison.InvariantCultureIgnoreCase ) );


    var moviesIds = availableMovies
        .Where( x => !string.IsNullOrWhiteSpace( x.JellyfinMediaId ) || !string.IsNullOrWhiteSpace( x.JellyfinMediaId4k ) )
        .Select( x => new
        {
            Id = string.IsNullOrWhiteSpace( x.JellyfinMediaId )
        ? Guid.Parse( x.JellyfinMediaId4k ).ToString( "d" )
        : Guid.Parse( x.JellyfinMediaId ).ToString( "d" ),
            MediaId = x.Id,
            TmdbId = x.TmdbId
        } );

    Console.WriteLine( "Total Jellyseerr Movies found as Available: " + moviesIds.Count() );


    var jellySearchResult = await jellyfinClient.GetFromJsonAsync<JellyfinSearchResult>( $"Items?ids={string.Join( ",", moviesIds.Select( x => x.Id ) )}&enableTotalRecordCount=false&enableImages=false" );

    var notFoundMovies = moviesIds.Where( x => !jellySearchResult.Items.Any( y => Guid.Parse( y.Id ) == Guid.Parse( x.Id ) ) );

    Console.WriteLine( "Jellyfin movies not found: " + notFoundMovies.Count() );

    //var links = notFoundMovies.Select( x => $"{JELLYSEERR_HOST_URL}movie/{x.TmdbId}" );
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
}