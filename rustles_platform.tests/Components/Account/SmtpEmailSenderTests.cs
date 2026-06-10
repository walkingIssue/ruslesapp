using System.Net;
using System.Net.Sockets;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using rustles.Infrastructure.Model;
using rustles_platform.Components.Account;

namespace rustles_platform.tests.Components.Account;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendConfirmationLinkAsync_sends_email_through_configured_smtp_server()
    {
        await using var smtpServer = await TestSmtpServer.StartAsync();
        var sender = new SmtpEmailSender(
            Options.Create(new SmtpEmailOptions
            {
                Host = IPAddress.Loopback.ToString(),
                Port = smtpServer.Port,
                EnableSsl = false,
                FromAddress = "no-reply@rustles.test",
                FromName = "Rustles Test"
            }),
            NullLogger<SmtpEmailSender>.Instance);

        await sender.SendConfirmationLinkAsync(
            new User { PermanentUsername = "testuser" },
            "person@example.test",
            "https://example.test/confirm?token=abc");

        var message = await smtpServer.GetMessageAsync();

        Assert.Contains("no-reply@rustles.test", message);
        Assert.Contains("To: person@example.test", message);
        Assert.Contains("Subject: Confirm your Rustles account", message);
        Assert.Contains("Content-Type: text/html", message);
    }

    private sealed class TestSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TaskCompletionSource<string> _message = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _serverTask;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public static Task<TestSmtpServer> StartAsync()
        {
            var server = new TestSmtpServer();
            server._listener.Start();
            server._serverTask = Task.Run(server.HandleConnectionAsync);
            return Task.FromResult(server);
        }

        public async Task<string> GetMessageAsync()
        {
            return await _message.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        private async Task HandleConnectionAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                await using var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
                {
                    AutoFlush = true,
                    NewLine = "\r\n"
                };

                await writer.WriteLineAsync("220 rustles.test SMTP ready");

                var data = new StringBuilder();
                var readingData = false;

                while (await reader.ReadLineAsync() is { } line)
                {
                    if (readingData)
                    {
                        if (line == ".")
                        {
                            _message.TrySetResult(data.ToString());
                            readingData = false;
                            await writer.WriteLineAsync("250 OK");
                            continue;
                        }

                        data.AppendLine(line);
                        continue;
                    }

                    if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250-rustles.test");
                        await writer.WriteLineAsync("250 OK");
                    }
                    else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 OK");
                    }
                    else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        readingData = true;
                        await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("221 Bye");
                        break;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 OK");
                    }
                }
            }
            catch (Exception ex)
            {
                _message.TrySetException(ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();

            if (_serverTask is null)
                return;

            try
            {
                await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
        }
    }
}
