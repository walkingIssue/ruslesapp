using System.ComponentModel.DataAnnotations;

namespace rustles_platform.Components.Account;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "Rustles";

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public bool UseDefaultCredentials { get; init; }
}
