using DeveloperKorea.ConsoleApp;
using DeveloperKorea.ConsoleApp.Configs;
using DeveloperKorea.ConsoleApp.Services;
using DeveloperKorea.ConsoleApp.Helpers;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var switches = new Dictionary<string, string>()
{
    { "--api-key", nameof(ArgumentSettings.ApiKey) },
    { "-c", nameof(ArgumentSettings.ChannelId) },
    { "--channel-id", nameof(ArgumentSettings.ChannelId) },
    { "-n", nameof(ArgumentSettings.PlaylistName) },
    { "--playlist-name", nameof(ArgumentSettings.PlaylistName) },
    { "-o", nameof(ArgumentSettings.OutputPath) },
    { "--output-path", nameof(ArgumentSettings.OutputPath) }
};

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
       .AddJsonFile("appsettings.json")
#if DEBUG
       .AddJsonFile("appsettings.Development.json")
#endif
       .AddCommandLine(args, switches);

builder.Services.AddHostedService<ProgramHost>()
                .AddScoped(p => p.GetRequiredService<IConfiguration>().Get<ArgumentSettings>())
                .AddScoped(p =>
                {
                    var settings = p.GetRequiredService<ArgumentSettings>();
                    var initialiser = new BaseClientService.Initializer()
                    {
                        ApplicationName = "Microsoft Developer Koera",
                        ApiKey = settings.ApiKey,
                    };

                    var youtube = new YouTubeService(initialiser);

                    return youtube;
                })
                .AddScoped<IYouTubeHelper, YouTubeHelper>()
                .AddScoped<IPlaylistService, PlaylistService>();

var host = builder.Build();

await host.RunAsync();
