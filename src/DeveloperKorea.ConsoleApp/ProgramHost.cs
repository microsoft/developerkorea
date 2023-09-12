using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using DeveloperKorea.ConsoleApp.Configs;
using DeveloperKorea.ConsoleApp.Services;

using Microsoft.Extensions.Hosting;

namespace DeveloperKorea.ConsoleApp;

public class ProgramHost : IHostedService
{
    private readonly ArgumentSettings _settings;
    private readonly IPlaylistService _service;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ProgramHost(ArgumentSettings settings, IPlaylistService service, IHostApplicationLifetime applicationLifetime)
    {
        this._settings = settings;
        this._service = service;
        this._applicationLifetime = applicationLifetime;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var playlists = await this._service.GetPlaylistsAsync(this._settings.ChannelId);
        var playlistId = playlists.SingleOrDefault(p => p.Title.Equals(this._settings.PlaylistName, StringComparison.InvariantCultureIgnoreCase))?.Id;
        var playlistItems = await this._service.GetPlaylistItemsAsync(playlistId);

        var serialised = JsonSerializer.Serialize(playlistItems, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });
#if DEBUG
        Console.WriteLine(serialised);
#endif
        await File.WriteAllTextAsync(this._settings.OutputPath, serialised);

        this._applicationLifetime.StopApplication();
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine(this._settings.Greetings);

        return Task.CompletedTask;
    }
}
