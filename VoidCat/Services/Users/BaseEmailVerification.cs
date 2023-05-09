using System.Net;
using System.Net.Mail;
using VoidCat.Database;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

/// <inheritdoc />
public abstract class BaseEmailVerification : IEmailVerification
{
    public const int HoursExpire = 1;
    private readonly VoidSettings _settings;
    private readonly ILogger<BaseEmailVerification> _logger;
    private readonly RazorPartialToStringRenderer _renderer;

    protected BaseEmailVerification(ILogger<BaseEmailVerification> logger, VoidSettings settings,
        RazorPartialToStringRenderer renderer)
    {
        _logger = logger;
        _settings = settings;
        _renderer = renderer;
    }

    /// <inheritdoc />
    public async ValueTask<EmailVerification> SendNewCode(User user)
    {
        var token = new EmailVerification{
            UserId = user.Id, 
            Code = Guid.NewGuid(), 
            Expires = DateTime.UtcNow.AddHours(HoursExpire)
        };
        await SaveToken(token);
        _logger.LogInformation("Saved email verification token for User={Id} Token={Token}", user.Id, token.Code);

        // send email
        try
        {
            var conf = _settings.Smtp;
            using var sc = new SmtpClient();
            sc.Host = conf?.Server?.Host!;
            sc.Port = conf?.Server?.Port ?? 25;
            sc.EnableSsl = conf?.Server?.Scheme == "tls";
            sc.Credentials = new NetworkCredential(conf?.Username, conf?.Password);

            var msgContent = await _renderer.RenderPartialToStringAsync("~/Pages/EmailCode.cshtml", token);
            var msg = new MailMessage();
            msg.From = new MailAddress(conf?.Username ?? "no-reply@void.cat");
            msg.To.Add(user.Email);
            msg.Subject = "Email verification code";
            msg.IsBodyHtml = true;
            msg.Body = msgContent;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(1));
            await sc.SendMailAsync(msg, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification code {Error}", ex.Message);
        }

        return token;
    }

    /// <inheritdoc />
    public async ValueTask<bool> VerifyCode(User user, Guid code)
    {
        var token = await GetToken(user.Id, code);
        if (token == default) return false;

        var isValid = user.Id == token.UserId &&
                      DateTime.SpecifyKind(token.Expires, DateTimeKind.Utc) > DateTimeOffset.UtcNow;
        if (isValid)
        {
            await DeleteToken(user.Id, code);
        }

        return isValid;
    }

    protected abstract ValueTask SaveToken(EmailVerification code);
    protected abstract ValueTask<EmailVerification?> GetToken(Guid user, Guid code);
    protected abstract ValueTask DeleteToken(Guid user, Guid code);
}