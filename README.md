# JellyseerrSync

## The Problem
If media gets deleted from jellyfin, jellyseerr does not get updated and so media still shows as Available. See https://github.com/Fallenbagel/jellyseerr/issues/84

This app provides kinda of an hacky way to keep jellyseerr synced, while jellyseerr does not provide a fix.

## Notifications
Clean up the availability on Jellyseerr by using the Radarr and Sonarr Webhook to listen to the **MovieFileDelete** and **EpisodeFileDelete** events.

### Radarr

Set up the webhook notification to listen to the Notification "On Movie File Delete" on **http://ip_or_url/radarr/notification**

### Sonarr

Set up the webhook notification to listen to the Notification "**On Series Delete**", "**On Episode File Delete**" on **http://ip_or_url/sonarr/notification**

It is of note that upon an episode being deleted, the entire series just gets cleared currently. As there seems to be no way to determine if there are episodes still left or not. Jellyseerr recurring Sonarr Scan
job should refresh any entry that might have been cleared, but it's still actually available.

## Sync
By visiting **http://ip_or_url/syncdeleted/movies** the app will query Jellyseerr for every movie that's marked as Available, and verify whether a corresponding item exists in the Jellyfin database. If it does not, it clears the movie entry on Jellyseerr. 
A log is provided with every movie entry that was cleared.

## How to Deploy
A docker image has been provided: 
https://hub.docker.com/r/dockerdaverick/jellyseerrsync

Example usage:

Docker-compose:
```
version: "3.9"

services:

  jellyseerr-sync:
    image: dockerdaverick/jellyseerrsync
    environment:
      - JELLYSEERR_APIKEY=MYAPIKEY
      - JELLYFIN_APIKEY=MYAPIKEY
      - JELLYSEERR_HOST_URL=http://192.168.1.11:5055/
      - JELLYFIN_HOST_URL=http://192.168.1.10:8096/
    ports:
      - 50580:80
    restart: unless-stopped
```

