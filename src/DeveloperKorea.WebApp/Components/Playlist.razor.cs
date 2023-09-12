using System.Net.Http.Json;

using DeveloperKorea.Models;

using Microsoft.AspNetCore.Components;

namespace DeveloperKorea.WebApp.Components;

public partial class Playlist : ComponentBase
{
    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Inject]
    protected HttpClient? Http { get; set; }

    protected List<PlaylistItem>? Items { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var deserialised = await Http.GetFromJsonAsync<List<PlaylistItem>>($"data/{this.Name}.json");
        this.Items = deserialised.OrderByDescending(p => p.PublishedAt).ToList();
    }
}
