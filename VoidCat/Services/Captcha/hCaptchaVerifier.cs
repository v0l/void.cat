using System.Net;
using Newtonsoft.Json;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Captcha;

public class hCaptchaVerifier : ICaptchaVerifier
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly VoidSettings _settings;

    public hCaptchaVerifier(IHttpClientFactory clientFactory, VoidSettings settings)
    {
        _clientFactory = clientFactory;
        _settings = settings;
    }

    public async ValueTask<bool> Verify(string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        
        var req = new HttpRequestMessage(HttpMethod.Post, "https://hcaptcha.com/siteverify");
        req.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("response", token),
            new KeyValuePair<string, string>("secret", _settings.CaptchaSettings!.Secret!)
        });

        using var cli = _clientFactory.CreateClient();
        var rsp = await cli.SendAsync(req);
        if (rsp.StatusCode == HttpStatusCode.OK)
        {
            var body = JsonConvert.DeserializeObject<hCaptchaResponse>(await rsp.Content.ReadAsStringAsync());
            return body?.Success == true;
        }

        return false;
    }

    internal sealed class hCaptchaResponse
    {
        public bool Success { get; init; }
    }
}