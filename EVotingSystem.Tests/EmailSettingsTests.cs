using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Tests;

public class EmailSettingsTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void SettingsService_RoundTripsEmailSettings()
    {
        var service = new SettingsService(_database.Factory);

        Assert.False(service.GetEmailSettings().IsConfigured);

        service.SaveEmailSettings(new EmailSettings
        {
            Enabled = true,
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            UseStartTls = true,
            Username = "user",
            Password = "secret",
            FromAddress = "elections@example.com",
            FromName = "Elections",
            PublicBaseUrl = "https://vote.example.com",
        });

        var loaded = service.GetEmailSettings();
        Assert.True(loaded.IsConfigured);
        Assert.Equal("smtp.example.com", loaded.SmtpHost);
        Assert.Equal(587, loaded.SmtpPort);
        Assert.Equal("elections@example.com", loaded.FromAddress);

        // Saving again updates the same single row.
        service.SaveEmailSettings(new EmailSettings { Enabled = false, SmtpHost = "smtp.example.com", SmtpPort = 25, FromAddress = "a@b.c" });
        var second = new SettingsService(_database.Factory).GetEmailSettings();
        Assert.False(second.IsConfigured);
        Assert.Equal(25, second.SmtpPort);
    }

    [Fact]
    public void SmtpInvitationSender_ReportsUnconfiguredWithoutSending()
    {
        var sender = new SmtpInvitationSender(new EmailSettings());

        Assert.False(sender.IsConfigured);

        var result = sender.Send(new[]
        {
            new InvitationRequest("Grace", "grace@example.com", null, "http://localhost:5024/invite/x"),
        });

        Assert.Equal(0, result.Sent);
        Assert.False(result.ProviderConfigured);
    }

    [Fact]
    public void Sender_IsConfiguredRequiresHostAndFrom()
    {
        Assert.False(new EmailSettings { Enabled = true, FromAddress = "a@b.c" }.IsConfigured);
        Assert.False(new EmailSettings { Enabled = true, SmtpHost = "smtp" }.IsConfigured);
        Assert.True(new EmailSettings { Enabled = true, SmtpHost = "smtp", FromAddress = "a@b.c" }.IsConfigured);
        Assert.False(new EmailSettings { Enabled = false, SmtpHost = "smtp", FromAddress = "a@b.c" }.IsConfigured);
    }

    public void Dispose() => _database.Dispose();
}
