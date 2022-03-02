using System.Net;
using System.Net.Mail;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.Users;

public class EmailVerification : IEmailVerification
{
    private readonly ICache _cache;
    private readonly VoidSettings _settings;
    private readonly ILogger<EmailVerification> _logger;
    private readonly RazorPartialToStringRenderer _renderer;

    public EmailVerification(ICache cache, ILogger<EmailVerification> logger, VoidSettings settings, RazorPartialToStringRenderer renderer)
    {
        _cache = cache;
        _logger = logger;
        _settings = settings;
        _renderer = renderer;
    }

    public async ValueTask<EmailVerificationCode> SendNewCode(PrivateVoidUser user)
    {
        const int codeExpire = 1;
        var code = new EmailVerificationCode()
        {
            UserId = user.Id,
            Expires = DateTimeOffset.UtcNow.AddHours(codeExpire)
        };
        await _cache.Set(MapToken(code.Id), code, TimeSpan.FromHours(codeExpire));
        _logger.LogInformation("Saved email verification token for User={Id} Token={Token}", user.Id, code.Id);
        
        // send email
        try
        {
            var conf = _settings.Smtp;
            using var sc = new SmtpClient();
            sc.Host = conf?.Server?.Host!;
            sc.Port = conf?.Server?.Port ?? 25;
            sc.EnableSsl = conf?.Server?.Scheme == "tls";
            sc.Credentials = new NetworkCredential(conf?.Username, conf?.Password);
            
            var msgContent = await _renderer.RenderPartialToStringAsync("~/Pages/EmailCode.cshtml", code);
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

        return code;
    }

    public async ValueTask<bool> VerifyCode(PrivateVoidUser user, Guid code)
    {
        var token = await _cache.Get<EmailVerificationCode>(MapToken(code));
        if (token == default) return false;
        
        var isValid = user.Id == token.UserId && token.Expires > DateTimeOffset.UtcNow;
        if (isValid)
        {
            await _cache.Delete(MapToken(code));
        }

        return isValid;
    }

    private static string MapToken(Guid id) => $"email-code:{id}";
}