using System.Text;
using Newtonsoft.Json;
using VoidCat.Model;

namespace VoidCat.Services.VirusScanner.VirusTotal;

public class VirusTotalClient
{
    private readonly HttpClient _client;

    public VirusTotalClient(IHttpClientFactory clientFactory, VirusTotalConfig config)
    {
        _client = clientFactory.CreateClient();

        _client.BaseAddress = new Uri("https://www.virustotal.com/");
        _client.DefaultRequestHeaders.Add("x-apikey", config.ApiKey);
        _client.DefaultRequestHeaders.Add("accept", "application/json");
    }

    public async Task<File?> GetReport(string id)
    {
        return await SendRequest<File>(HttpMethod.Get, $"/api/v3/files/{id}");
    }

    private Task<TResponse> SendRequest<TResponse>(HttpMethod method, string path)
    {
        return SendRequest<TResponse, object>(method, path);
    }
    
    private async Task<TResponse> SendRequest<TResponse, TRequest>(HttpMethod method, string path, TRequest? body = null)
        where TRequest : class
    {
        var req = new HttpRequestMessage(method, path);
        if (body != default)
        {
            var json = JsonConvert.SerializeObject(body);
            req.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        }

        var rsp = await _client.SendAsync(req);
        var rspBody = await rsp.Content.ReadAsStringAsync();
        var vtResponse = JsonConvert.DeserializeObject<VTResponse<TResponse>>(rspBody);
        if (vtResponse == default) throw new Exception("Failed?");
        if (vtResponse.Error != default) throw new VTException(vtResponse.Error);
        
        return vtResponse.Data;
    }
}