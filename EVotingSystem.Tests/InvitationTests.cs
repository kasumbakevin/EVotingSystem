using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EVotingSystem.Tests;

public class InvitationTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void SendInvitations_GeneratesTokensAndExportsWhenProviderMissing()
    {
        var voterId = _database.AddVoter("Grace Hopper", "grace", "V900");
        var exportDirectory = Path.Combine(Path.GetTempPath(), $"evoting_invites_{Guid.NewGuid():N}");
        var service = new InvitationService(
            _database.Factory,
            new CsvExportInvitationSender(exportDirectory),
            "http://localhost:5024");

        var result = service.SendInvitations(new[] { voterId });

        Assert.False(result.ProviderConfigured);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Sent);
        Assert.True(File.Exists(result.ExportPath));

        using var context = _database.Factory.CreateContext();
        var voter = context.Voters.Single(v => v.Id == voterId);
        Assert.False(string.IsNullOrWhiteSpace(voter.InvitationToken));
        Assert.NotNull(voter.InvitationExpiresAt);
        Assert.Null(voter.InvitationSentAt);
    }

    [Fact]
    public void ResolveInvitation_ReturnsVoterForValidToken()
    {
        var voterId = _database.AddVoter("Alan Turing", "alan", "V901");
        var service = new InvitationService(
            _database.Factory,
            new CsvExportInvitationSender(Path.Combine(Path.GetTempPath(), $"evoting_invites_{Guid.NewGuid():N}")));

        var result = service.SendInvitations(new[] { voterId });

        using var context = _database.Factory.CreateContext();
        var token = context.Voters.Single(v => v.Id == voterId).InvitationToken!;

        var resolved = service.ResolveInvitation(token);
        Assert.NotNull(resolved);
        Assert.Equal(voterId, resolved!.Id);
        Assert.NotNull(result.ExportPath);
    }

    [Fact]
    public void ResolveInvitation_ReturnsNullForUnknownToken()
    {
        var service = new InvitationService(
            _database.Factory,
            new CsvExportInvitationSender(Path.Combine(Path.GetTempPath(), $"evoting_invites_{Guid.NewGuid():N}")));

        Assert.Null(service.ResolveInvitation("not-a-real-token"));
    }

    public void Dispose() => _database.Dispose();
}

public class SchemaUpgradeTests
{
    [Fact]
    public void Initialize_AddsInvitationColumnsToLegacyDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"evoting_legacy_{Guid.NewGuid():N}.db");

        var factory = new DatabaseFactory(path);
        factory.Initialize();

        // Simulate a database created before the invitation columns existed.
        using (var context = factory.CreateContext())
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE Voters DROP COLUMN InvitationToken;");
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE Voters DROP COLUMN InvitationExpiresAt;");
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE Voters DROP COLUMN InvitationSentAt;");
        }

        factory.Initialize();

        using (var context = factory.CreateContext())
        {
            var columns = context.Database
                .SqlQuery<string>($"SELECT name FROM pragma_table_info('Voters')")
                .ToList();

            Assert.Contains("InvitationToken", columns);
            Assert.Contains("InvitationExpiresAt", columns);
            Assert.Contains("InvitationSentAt", columns);
        }

        factory.Dispose();
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
