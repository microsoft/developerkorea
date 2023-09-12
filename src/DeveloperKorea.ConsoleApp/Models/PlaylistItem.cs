namespace DeveloperKorea.ConsoleApp.Models;

public class PlaylistItem : DeveloperKorea.Models.PlaylistItem
{
    public PlaylistItem(Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem)
    {
        this.Id = playlistItem.Id;
        this.VideoId = playlistItem.ContentDetails.VideoId;
        this.Title = playlistItem.Snippet.Title;
        this.Description = playlistItem.Snippet.Description;
        this.PublishedAt = playlistItem.ContentDetails.VideoPublishedAtDateTimeOffset;
        this.Thumbnail = new Thumbnail(playlistItem.Snippet.Thumbnails.Maxres);
    }
}
