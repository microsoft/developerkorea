using System.Threading.Channels;
using DeveloperKorea.ConsoleApp.Configs;

using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace DeveloperKorea.ConsoleApp.Helpers;

public interface IYouTubeHelper
{
    Task<IList<Playlist>> GetPlaylistsAsync(string channelId);

    Task<IList<PlaylistItem>> GetPlaylistItemsAsync(string playlistId);
}

public class YouTubeHelper : IYouTubeHelper
{
    private readonly YouTubeService _youtube;

    public YouTubeHelper(YouTubeService youtube)
        => this._youtube = youtube ?? throw new ArgumentNullException(nameof(youtube));

    public async Task<IList<Playlist>> GetPlaylistsAsync(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentNullException(nameof(channelId));
        }

        var list = this._youtube.Playlists.List(new Repeatable<string>(new[] { "snippet" }));
        list.ChannelId = channelId;
        list.MaxResults = 50;

        var result = await list.ExecuteAsync().ConfigureAwait(false);

        return result.Items;
    }

    public async Task<IList<PlaylistItem>> GetPlaylistItemsAsync(string playlistId)
    {
        if (string.IsNullOrWhiteSpace(playlistId))
        {
            throw new ArgumentNullException(nameof(playlistId));
        }

        var list = this._youtube.PlaylistItems.List(new Repeatable<string>(new[] { "snippet", "contentDetails", "status" }));
        list.PlaylistId = playlistId;
        list.MaxResults = 50;

        var result = await list.ExecuteAsync().ConfigureAwait(false);

        return result.Items;
    }
}
