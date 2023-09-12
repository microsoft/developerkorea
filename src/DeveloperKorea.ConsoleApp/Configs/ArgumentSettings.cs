namespace DeveloperKorea.ConsoleApp.Configs;

public class ArgumentSettings
{
    public string? ApiKey { get; set; }
    public string? ChannelId { get; set; }
    public string? PlaylistName { get; set; }
    public string? OutputPath { get; set; } = "playlist.json";
    public string? Greetings { get; set; } = "Hello!";
}
