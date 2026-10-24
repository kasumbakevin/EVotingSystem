using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Tests;

public class ElectionLifecycleTests
{
    [Fact]
    public void StartElection_requires_candidates()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Empty", "Chair", ElectionStatus.Draft);
        db.AddVoter("Bob", "bob", "V100");

        var result = new ElectionService(db.Factory).StartElection(electionId, adminId: 1);

        Assert.False(result.Success);
        Assert.Contains("no candidates", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartElection_requires_verified_eligible_voters()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("No Voters", "Chair", ElectionStatus.Draft);
        db.AddCandidate(electionId, "Alice", "Chair");
        db.AddVoter("Bob", "bob", "V100", VoterVerificationStatus.PendingVerification);

        var result = new ElectionService(db.Factory).StartElection(electionId, adminId: 1);

        Assert.False(result.Success);
        Assert.Contains("eligible voters", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartElection_opens_and_records_who_and_when()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Ready", "Chair", ElectionStatus.Draft);
        db.AddCandidate(electionId, "Alice", "Chair");
        db.AddVoter("Bob", "bob", "V100");

        var result = new ElectionService(db.Factory).StartElection(electionId, adminId: 1);

        Assert.True(result.Success);
        using var context = db.Factory.CreateContext();
        var election = context.Elections.Single(e => e.Id == electionId);
        Assert.Equal(ElectionStatus.Open, election.Status);
        Assert.Equal(1, election.OpenedByAdminId);
        Assert.NotNull(election.OpenedAt);
    }

    [Fact]
    public void Closed_election_cannot_reopen()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Closed", "Chair", ElectionStatus.Closed);
        db.AddCandidate(electionId, "Alice", "Chair");
        db.AddVoter("Bob", "bob", "V100");

        var result = new ElectionService(db.Factory).StartElection(electionId, adminId: 1);

        Assert.False(result.Success);
        Assert.Contains("cannot be reopened", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pause_then_resume_transitions_correctly()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Live", "Chair", ElectionStatus.Open);
        var service = new ElectionService(db.Factory);

        Assert.True(service.PauseElection(electionId).Success);
        Assert.Equal(ElectionStatus.Paused, StatusOf(db, electionId));

        Assert.True(service.ResumeElection(electionId).Success);
        Assert.Equal(ElectionStatus.Open, StatusOf(db, electionId));
    }

    [Fact]
    public void Pause_rejects_non_open_election()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Draft", "Chair", ElectionStatus.Draft);

        var result = new ElectionService(db.Factory).PauseElection(electionId);

        Assert.False(result.Success);
    }

    [Fact]
    public void Starting_one_election_does_not_change_another()
    {
        using var db = new TestDatabase();
        int first = db.AddElection("First", "Chair", ElectionStatus.Draft);
        int second = db.AddElection("Second", "Chair", ElectionStatus.Draft);
        db.AddCandidate(first, "Alice", "Chair");
        db.AddVoter("Bob", "bob", "V100");

        var service = new ElectionService(db.Factory);
        Assert.True(service.StartElection(first, adminId: 1).Success);

        Assert.Equal(ElectionStatus.Open, StatusOf(db, first));
        Assert.Equal(ElectionStatus.Draft, StatusOf(db, second));
    }

    [Fact]
    public void CloseExpiredElections_closes_past_deadlines_only()
    {
        using var db = new TestDatabase();
        int expired = db.AddElection("Expired", "Chair", ElectionStatus.Open);
        int live = db.AddElection("Live", "Chair", ElectionStatus.Open);
        using (var context = db.Factory.CreateContext())
        {
            context.Elections.Single(e => e.Id == expired).ClosesAt = DateTime.UtcNow.AddMinutes(-5);
            context.Elections.Single(e => e.Id == live).ClosesAt = DateTime.UtcNow.AddMinutes(30);
            context.SaveChanges();
        }

        int closed = new ElectionService(db.Factory).CloseExpiredElections();

        Assert.Equal(1, closed);
        Assert.Equal(ElectionStatus.Closed, StatusOf(db, expired));
        Assert.Equal(ElectionStatus.Open, StatusOf(db, live));
    }

    private static ElectionStatus StatusOf(TestDatabase db, int electionId)
    {
        using var context = db.Factory.CreateContext();
        return context.Elections.Single(e => e.Id == electionId).Status;
    }
}
