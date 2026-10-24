using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Tests;

/// <summary>
/// Provides an isolated SQLite database per test, disposed when the test ends.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public DatabaseFactory Factory { get; }
    private readonly string _path;

    public TestDatabase()
    {
        _path = Path.Combine(Path.GetTempPath(), $"evoting_test_{Guid.NewGuid():N}.db");
        Factory = new DatabaseFactory(_path);
        Factory.Initialize();
    }

    public int AddElection(string title, string position, ElectionStatus status = ElectionStatus.Open)
    {
        using var context = Factory.CreateContext();
        var election = new Election(0, title, position) { Status = status };
        if (status == ElectionStatus.Open)
        {
            election.OpenedAt = DateTime.UtcNow;
        }
        context.Elections.Add(election);
        context.SaveChanges();
        return election.Id;
    }

    public int AddCandidate(int electionId, string name, string position, string party = "Independent")
    {
        using var context = Factory.CreateContext();
        var candidate = new Candidate(0, name, position, party) { ElectionId = electionId };
        context.Candidates.Add(candidate);
        context.SaveChanges();
        return candidate.Id;
    }

    public int AddVoter(string name, string username, string voterNumber, VoterVerificationStatus status = VoterVerificationStatus.Verified)
    {
        using var context = Factory.CreateContext();
        var voter = new Voter(0, name, username, string.Empty, voterNumber)
        {
            VerificationStatus = status,
            IsActive = true,
        };
        voter.PasswordHash = SeedData.HashPassword(voter, "Password123!");
        context.Voters.Add(voter);
        context.SaveChanges();
        return voter.Id;
    }

    public void Dispose()
    {
        Factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
        catch (IOException)
        {
        }
    }
}
