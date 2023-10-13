using Newtonsoft.Json;
using Nostr.Client.Json;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Metadata;
using VoidCat.Model;

namespace VoidCat.Services.Users;

public class NostrProfileService
{
    private readonly HttpClient _client;
    private readonly VoidSettings _settings;

    public NostrProfileService(HttpClient client, VoidSettings settings)
    {
        _client = client;
        _settings = settings;
        _client.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<NostrMetadata?> FetchProfile(string pubkey)
    {
        try
        {
            var req = await _client.GetAsync($"https://api.snort.social/api/v1/raw/p/{pubkey}");
            if (req.IsSuccessStatusCode)
            {
                var ev = JsonConvert.DeserializeObject<NostrEvent>(await req.Content.ReadAsStringAsync(), NostrSerializer.Settings);
                if (ev != default)
                {
                    return JsonConvert.DeserializeObject<NostrMetadata>(ev.Content!, NostrSerializer.Settings);
                }
            }
        }
        catch (Exception ex)
        {
            // ignored
        }

        return default;
    }
}
