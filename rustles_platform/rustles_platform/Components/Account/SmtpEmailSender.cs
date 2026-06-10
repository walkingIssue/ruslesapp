using System.Net;
using System.Net.Mail;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using rustles.Infrastructure.Model;
using rustles_platform.Services;

namespace rustles_platform.Components.Account;

public sealed class SmtpEmailSender : IEmailSender<User>
{
    private readonly IOptions<SmtpEmailOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var displayName = WebUtility.HtmlEncode(UserDisplayName.Resolve(user));
        var link = WebUtility.HtmlEncode(confirmationLink);

        return SendHtmlEmailAsync(
            email,
            "Confirm your Rustles account",
            $"""
            <p>Hello {displayName},</p>
            <p>Please confirm your Rustles account by opening this link:</p>
            <p><a href="{link}">Confirm your email</a></p>
            """);
    }

    public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var displayName = WebUtility.HtmlEncode(UserDisplayName.Resolve(user));
        var link = WebUtility.HtmlEncode(resetLink);

        return SendHtmlEmailAsync(
            email,
            "Reset your Rustles password",
            $"""
            <p>Hello {displayName},</p>
            <p>You can reset your Rustles password by opening this link:</p>
            <p><a href="{link}">Reset your password</a></p>
            """);
    }

    public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var displayName = WebUtility.HtmlEncode(UserDisplayName.Resolve(user));
        var code = WebUtility.HtmlEncode(resetCode);

        return SendHtmlEmailAsync(
            email,
            "Reset your Rustles password",
            $"""
            <p>Hello {displayName},</p>
            <p>Use this code to reset your Rustles password:</p>
            <p><code>{code}</code></p>
            """);
    }

    private async Task SendHtmlEmailAsync(string email, string subject, string body)
    {
        var options = _options.Value;

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            UseDefaultCredentials = options.UseDefaultCredentials
        };

        if (!options.UseDefaultCredentials && !string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials = new NetworkCredential(options.UserName, options.Password);
        }

        await client.SendMailAsync(message);

        _logger.LogInformation("Sent Identity email with subject '{Subject}'.", subject);
    }
}
