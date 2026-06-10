# Mail Sender Configuration

Rustles uses `SmtpEmailSender` for ASP.NET Core Identity email flows:

- account confirmation
- password reset links
- password reset codes
- email-change confirmation

The sender is registered as `IEmailSender<User>` in `rustles_platform/rustles_platform/Program.cs`.

Configuration is read from the `Smtp` section:

```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "FromAddress": "no-reply@example.com",
    "FromName": "Rustles",
    "UserName": "smtp-user",
    "Password": "smtp-password",
    "UseDefaultCredentials": false
  }
}
```

Do not commit real SMTP credentials. For production, set them through environment variables or user secrets:

```powershell
$env:Smtp__Host = "smtp.example.com"
$env:Smtp__Port = "587"
$env:Smtp__EnableSsl = "true"
$env:Smtp__FromAddress = "no-reply@example.com"
$env:Smtp__FromName = "Rustles"
$env:Smtp__UserName = "smtp-user"
$env:Smtp__Password = "smtp-password"
```

For local development, `appsettings.Development.json` points at `localhost:1025` with TLS disabled. That works with local SMTP catchers such as Mailpit or MailHog. Seeded development users are still the easiest way to test the chat flow without depending on email delivery.
