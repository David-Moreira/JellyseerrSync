# JellyseerrSync

## The Problem
If media gets deleted from jellyfin, jellyseerr does not get updated and so media still shows as Available. See https://github.com/Fallenbagel/jellyseerr/issues/84
This app provides kinda of an hacky way to keep jellyseerr synced, while jellyseerr does not provide a fix.

## Notifications
Clean up the availability on Jellyseerr by using the Radarr and Sonarr Webhook to listen to the MovieFileDelete and EpisodeFileDelete events.

### Radarr

Set up the webhook notification to listen to the Notification "On Movie File Delete" on http://ip_or_name/radarr/notification

### Sonarr

Set up the webhook notification to listen to the Notification "On Series Delete", "On Episode File Delete" on http://ip_or_name/sonarr/notification

## Sync
By visiting http://ip_or_name/syncdeleted/movies the app will query Jellyseerr for every movie, and verify whether an item exists in the Jellyfin database. If it does not, it clears the entry on Jellyseerr.
