using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VoidCat.Services;

public class StrikeApi
{
    private readonly ILogger<StrikeApi> _logger;
    private readonly HttpClient _client;
    private readonly StrikeApiSettings _settings;

    public StrikeApi(StrikeApiSettings settings, ILogger<StrikeApi> logger)
    {
        _client = new HttpClient
        {
            BaseAddress = settings.Uri ?? new Uri("https://api.strike.me/")
        };

        _settings = settings;
        _logger = logger;

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
    }

    public Task<Invoice?> GenerateInvoice(CreateInvoiceRequest invoiceRequest)
    {
        var path = !string.IsNullOrEmpty(invoiceRequest.Handle)
            ? $"/v1/invoices/handle/{invoiceRequest.Handle}"
            : "/v1/invoices";

        return SendRequest<Invoice>(HttpMethod.Post, path, invoiceRequest);
    }

    public Task<Profile?> GetProfile(string handle)
    {
        return SendRequest<Profile>(HttpMethod.Get, $"/v1/accounts/handle/{handle}/profile");
    }

    public Task<Profile?> GetProfile(Guid id)
    {
        return SendRequest<Profile>(HttpMethod.Get, $"/v1/accounts/{id}/profile");
    }

    public Task<Invoice?> GetInvoice(Guid id)
    {
        return SendRequest<Invoice>(HttpMethod.Get, $"/v1/invoices/{id}");
    }

    public Task<InvoiceQuote?> GetInvoiceQuote(Guid id)
    {
        return SendRequest<InvoiceQuote>(HttpMethod.Post, $"/v1/invoices/{id}/quote");
    }

    public Task<IEnumerable<WebhookSubscription>?> GetWebhookSubscriptions()
    {
        return SendRequest<IEnumerable<WebhookSubscription>>(HttpMethod.Get, "/v1/subscriptions");
    }

    public Task<WebhookSubscription?> CreateWebhook(NewWebhook hook)
    {
        return SendRequest<WebhookSubscription>(HttpMethod.Post, "/v1/subscriptions", hook);
    }

    public Task DeleteWebhook(Guid id)
    {
        return SendRequest<object>(HttpMethod.Delete, $"/v1/subscriptions/{id}");
    }

    private async Task<TReturn?> SendRequest<TReturn>(HttpMethod method, string path, object? bodyObj = default)
        where TReturn : class
    {
        var request = new HttpRequestMessage(method, path);
        if (bodyObj != default)
        {
            var reqJson = JsonConvert.SerializeObject(bodyObj);
            request.Content = new StringContent(reqJson, Encoding.UTF8, "application/json");
        }

        var rsp = await _client.SendAsync(request);
        var okResponse = method.Method switch
        {
            "POST" => HttpStatusCode.Created,
            _ => HttpStatusCode.OK
        };

        var json = await rsp.Content.ReadAsStringAsync();
        _logger.LogInformation(json);
        return rsp.StatusCode == okResponse ? JsonConvert.DeserializeObject<TReturn>(json) : default;
    }
}

public class Profile
{
    [JsonProperty("handle")] public string Handle { get; init; } = null;

    [JsonProperty("avatarUrl")] public string? AvatarUrl { get; init; }

    [JsonProperty("description")] public string? Description { get; init; }

    [JsonProperty("canReceive")] public bool CanReceive { get; init; }

    [JsonProperty("currencies")] public List<AvailableCurrency> Currencies { get; init; } = new();
}

public class InvoiceQuote
{
    [JsonProperty("quoteId")] public Guid QuoteId { get; init; }

    [JsonProperty("description")] public string? Description { get; init; }

    [JsonProperty("lnInvoice")] public string? LnInvoice { get; init; }

    [JsonProperty("onchainAddress")] public string? OnChainAddress { get; init; }

    [JsonProperty("expiration")] public DateTimeOffset Expiration { get; init; }

    [JsonProperty("expirationInSec")] public ulong ExpirationSec { get; init; }

    [JsonProperty("targetAmount")] public CurrencyAmount? TargetAmount { get; init; }

    [JsonProperty("sourceAmount")] public CurrencyAmount? SourceAmount { get; init; }

    [JsonProperty("conversionRate")] public ConversionRate? ConversionRate { get; init; }
}

public class ConversionRate
{
    [JsonProperty("amount")] public string? Amount { get; init; }

    [JsonProperty("sourceCurrency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies Source { get; init; }

    [JsonProperty("targetCurrency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies Target { get; init; }
}

public class ErrorResponse : Exception
{
    public ErrorResponse(string message) : base(message)
    {
    }
}

public class CreateInvoiceRequest
{
    [JsonProperty("correlationId")] public string? CorrelationId { get; init; }

    [JsonProperty("description")] public string? Description { get; init; }

    [JsonProperty("amount")] public CurrencyAmount? Amount { get; init; }

    [JsonProperty("handle")] public string? Handle { get; init; }
}

public class CurrencyAmount
{
    [JsonProperty("amount")] public string? Amount { get; init; }

    [JsonProperty("currency")]
    [JsonConverter(typeof(StringEnumConverter))]
    public Currencies? Currency { get; init; }
}

public class AvailableCurrency
{
    [JsonProperty("currency")] public Currencies Currency { get; init; }

    [JsonProperty("isDefaultCurrency")] public bool IsDefault { get; init; }

    [JsonProperty("isAvailable")] public bool IsAvailable { get; init; }
}

public enum Currencies
{
    BTC,
    USD,
    EUR,
    GBP,
    USDT
}

public class Invoice
{
    [JsonProperty("invoiceId")] public Guid InvoiceId { get; init; }

    [JsonProperty("amount")] public CurrencyAmount? Amount { get; init; }

    [JsonProperty("state")]
    [JsonConverter(typeof(StringEnumConverter))]
    public InvoiceState State { get; set; }

    [JsonProperty("created")] public DateTimeOffset? Created { get; init; }

    [JsonProperty("correlationId")] public string? CorrelationId { get; init; }

    [JsonProperty("description")] public string? Description { get; init; }

    [JsonProperty("issuerId")] public Guid? IssuerId { get; init; }

    [JsonProperty("receiverId")] public Guid? ReceiverId { get; init; }

    [JsonProperty("payerId")] public Guid? PayerId { get; init; }
}

public abstract class WebhookBase
{
    [JsonProperty("webhookUrl")] public Uri? Uri { get; init; }

    [JsonProperty("webhookVersion")] public string? Version { get; init; }

    [JsonProperty("enabled")] public bool? Enabled { get; init; }

    [JsonProperty("eventTypes")] public HashSet<string>? EventTypes { get; init; }
}

public sealed class NewWebhook : WebhookBase
{
    [JsonProperty("secret")] public string? Secret { get; init; }
}

public sealed class WebhookSubscription : WebhookBase
{
    [JsonProperty("id")] public Guid? Id { get; init; }

    [JsonProperty("created")] public DateTimeOffset? Created { get; init; }
}

public class WebhookData
{
    [JsonProperty("entityId")] public Guid? EntityId { get; set; }

    [JsonProperty("changes")] public List<string>? Changes { get; set; }
}

public class WebhookEvent
{
    [JsonProperty("id")] public Guid? Id { get; set; }

    [JsonProperty("eventType")] public string? EventType { get; set; }

    [JsonProperty("webhookVersion")] public string? WebhookVersion { get; set; }

    [JsonProperty("data")] public WebhookData? Data { get; set; }

    [JsonProperty("created")] public DateTimeOffset? Created { get; set; }

    [JsonProperty("deliverySuccess")] public bool? DeliverySuccess { get; set; }

    public override string ToString()
    {
        return $"Id = {Id}, EntityId = {Data?.EntityId}, Event = {EventType}";
    }
}

public enum InvoiceState
{
    UNPAID,
    PENDING,
    PAID,
    CANCELLED
}

public class StrikeApiSettings
{
    public Uri? Uri { get; init; }
    public string? ApiKey { get; init; }
}