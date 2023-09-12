namespace DeveloperKorea.ConsoleApp.Models;

public class Thumbnail : DeveloperKorea.Models.Thumbnail
{
    public Thumbnail(Google.Apis.YouTube.v3.Data.Thumbnail thumbnail)
    {
        this.Url = thumbnail.Url;
        this.Width = thumbnail.Width;
        this.Height = thumbnail.Height;
    }
}
