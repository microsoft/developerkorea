namespace DeveloperKorea.ConsoleApp.Models;

public class Playlist : DeveloperKorea.Models.Playlist
{
    public Playlist(Google.Apis.YouTube.v3.Data.Playlist playlist)
    {
        this.Id = playlist.Id;
        this.Title = playlist.Snippet.Title;
    }
}
