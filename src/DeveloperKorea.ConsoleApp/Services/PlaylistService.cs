using DeveloperKorea.ConsoleApp.Configs;
using DeveloperKorea.ConsoleApp.Models;
using DeveloperKorea.ConsoleApp.Helpers;

namespace DeveloperKorea.ConsoleApp.Services;

public interface IPlaylistService
{
    Task<List<Playlist>> GetPlaylistsAsync(string channelId);

    Task<List<PlaylistItem>> GetPlaylistItemsAsync(string playlistId);
}

public class PlaylistService : IPlaylistService
{
    private readonly ArgumentSettings _settings;
    private readonly IYouTubeHelper _helper;

    public PlaylistService(ArgumentSettings settings, IYouTubeHelper helper)
    {
        this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this._helper = helper ?? throw new ArgumentNullException(nameof(helper));
    }

    public async Task<List<Playlist>> GetPlaylistsAsync(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentNullException(nameof(channelId));
        }

        var results = await this._helper
                                .GetPlaylistsAsync(channelId)
                                .ConfigureAwait(false);
        var selected = results.Select(p => new Playlist(p))
                              .ToList();

        return selected;
    }

    public async Task<List<PlaylistItem>> GetPlaylistItemsAsync(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentNullException(nameof(playlistId));
        }

        var results = await this._helper
                                .GetPlaylistItemsAsync(playlistId)
                                .ConfigureAwait(false);
        var selected = results.Where(p => p.Status.PrivacyStatus.Equals("public", StringComparison.InvariantCultureIgnoreCase))
                              .Select(p => new PlaylistItem(p))
                              .OrderByDescending(p => p.PublishedAt)
                              .ToList();

        return selected;
    }
}
