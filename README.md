# JellyseerrSync

## Archived

With the release of jellyseer v1.8.0 https://github.com/Fallenbagel/jellyseerr/releases/tag/v1.8.0 a feature was implemented to properly remove media automatically on scan : Fallenbagel/jellyseerr#522
This means that this repository / application should no longer be needed from jellyseerr v1.8.0+.
This repository will no longer have updates and it is archived. All the docker images will still be available for usage.

## The Problem
If media gets deleted from jellyfin, jellyseerr does not get updated and so media still shows as Available. See https://github.com/Fallenbagel/jellyseerr/issues/84

This app provides kinda of an hacky way to keep jellyseerr synced, while jellyseerr does not provide a fix.

## Notifications
Clean up the availability on Jellyseerr by using the Radarr and Sonarr Webhook to listen to the **MovieFileDelete** and **EpisodeFileDelete** events.

### Radarr

Set up the webhook notification to listen to the Notification "**On Movie File Delete**" on **http://ip_or_url/radarr/notification**

### Sonarr

Set up the webhook notification to listen to the Notification "**On Series Delete**", "**On Episode File Delete**" on **http://ip_or_url/sonarr/notification**

It is of note that upon an episode being deleted, the entire series just gets cleared currently. As there seems to be no way to determine if there are episodes still left or not. Jellyseerr recurring Sonarr Scan
job should refresh any entry that might have been cleared, but it's still actually available.

## Sync
By visiting **http://ip_or_url/syncdeleted/movies** the app will query Jellyseerr for every movie that's marked as Available, and verify whether a corresponding item exists in the Jellyfin database. If it does not, it clears the movie entry on Jellyseerr. 
A log is provided with every movie entry that was cleared.

## Logs
Logs are provided on the root of the app if you use the default configuration, and can be accessed by visiting **http://ip_or_url/logs** or the file **JellyseerrSync.log**.
You can choose not to log to file by 
- Not providing the Logging variables
- Setting the environment variable **Logging:File:Path** to an empty string or not providing it at all.
- Setting the Logging:File:MinLevel to None

## How to Deploy
A docker image has been provided: 
https://hub.docker.com/r/dockerdaverick/jellyseerrsync

Example usage:

Docker-compose:
```
version: "3.9"
name: jellyseerr-notifications
services:

  jellyseerr-notifications:
    image: dockerdaverick/jellyseerrsync:latest
    environment:
      # Refer to https://github.com/nreco/logging for logging configuration
      - Logging:File:Path=JellyseerrSync.log 
      - Logging:File:Append=true
      - Logging:File:MinLevel=Information # min level for the file logger (Trace,Debug,Information,Warning,Error,Critical,None)
      - Logging:File:FileSizeLimitBytes=0 # use to activate rolling file behaviour
      - Logging:File:MaxRollingFiles=0 # use to specify max number of log files
      - JELLYSEERR_APIKEY=MYAPIKEY
      - JELLYFIN_APIKEY=MYAPIKEY
      - JELLYSEERR_HOST_URL=http://192.168.1.11:5055/
      - JELLYFIN_HOST_URL=http://192.168.1.10:8096/
    ports:
      - 50580:80
    restart: unless-stopped   
```

