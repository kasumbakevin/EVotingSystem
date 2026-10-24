using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Tests;

/// <summary>
/// Walks the full path a real operator takes: register a voter, verify them,
/// configure and start an election, then cast a ballot.
/// </summary>
public class EndToEndWorkflowTests
{
    [Fact]
    public void Register_verify_start_vote_succeeds()
    {
        using var db = new TestDatabase();
        var voterService = new VoterService(db.Factory);
        var electionService = new ElectionService(db.Factory);
        var votingService = new VotingService(db.Factory);

        // Register (starts Pending Verification).
        var registration = voterService.Register("Grace Okello", "grace", "Password123!", "V900", "Computer Science");
        Assert.True(registration.Success);

        var voter = voterService.GetAll().Single(v => v.Username == "grace");
        Assert.Equal(VoterVerificationStatus.PendingVerification, voter.VerificationStatus);

        // Cannot start an election: no candidates and no verified voters yet.
        int electionId = db.AddElection("Guild 2026", "President", ElectionStatus.Draft);
        Assert.False(electionService.StartElection(electionId, adminId: 1).Success);

        db.AddCandidate(electionId, "Alice Nabirye", "President");

        // Still blocked: voter not verified.
        var blocked = electionService.StartElection(electionId, adminId: 1);
        Assert.False(blocked.Success);
        Assert.Contains("eligible voters", blocked.Message, StringComparison.OrdinalIgnoreCase);

        // Verify the voter, then starting succeeds.
        Assert.True(voterService.SetVerification(voter.Id, VoterVerificationStatus.Verified).Success);
        var started = electionService.StartElection(electionId, adminId: 1);
        Assert.True(started.Success);

        // Vote now works.
        int candidateId = db.Factory.CreateContext().Candidates.Single(c => c.ElectionId == electionId).Id;
        var vote = votingService.CastVote(voter.Id, candidateId);
        Assert.True(vote.Success);

        using var context = db.Factory.CreateContext();
        Assert.Equal(1, context.Ballots.Count());
        Assert.True(context.Voters.Single(v => v.Id == voter.Id).HasVoted);
    }

    [Fact]
    public void VoterService_rejects_duplicate_voter_number()
    {
        using var db = new TestDatabase();
        var voterService = new VoterService(db.Factory);

        Assert.True(voterService.Register("First", "first", "Password123!", "V1").Success);
        var duplicate = voterService.Register("Second", "second", "Password123!", "V1");

        Assert.False(duplicate.Success);
        Assert.Contains("voter number", duplicate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deactivated_voter_cannot_vote_even_when_verified()
    {
        using var db = new TestDatabase();
        var voterService = new VoterService(db.Factory);

        voterService.Register("Grace", "grace", "Password123!", "V901");
        var voter = voterService.GetAll().Single(v => v.Username == "grace");
        voterService.SetVerification(voter.Id, VoterVerificationStatus.Verified);

        int electionId = db.AddElection("Guild", "President");
        int candidateId = db.AddCandidate(electionId, "Alice", "President");

        voterService.SetActive(voter.Id, false);

        var result = new VotingService(db.Factory).CastVote(voter.Id, candidateId);
        Assert.False(result.Success);
        Assert.Contains("not active", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
