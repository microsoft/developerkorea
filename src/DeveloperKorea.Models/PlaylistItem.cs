namespace DeveloperKorea.Models;

public class PlaylistItem
{
    public virtual string? Id { get; set; }
    public virtual string? VideoId { get; set; }
    public virtual string? Title { get; set; }
    public virtual string? Description { get; set; }
    public virtual DateTimeOffset? PublishedAt { get; set; }
    public virtual Thumbnail? Thumbnail { get; set; }
}
