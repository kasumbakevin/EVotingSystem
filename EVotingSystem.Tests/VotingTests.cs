using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Tests;

public class VotingTests
{
    [Fact]
    public void CastVote_succeeds_when_election_is_open_and_voter_verified()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President");
        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        var service = new VotingService(db.Factory);
        var result = service.CastVote(voterId, candidateId);

        Assert.True(result.Success);

        using var context = db.Factory.CreateContext();
        Assert.Equal(1, context.Ballots.Count());
        Assert.Equal(1, context.VoteReceipts.Count());
    }

    [Fact]
    public void CastVote_rejects_duplicate_vote_for_same_position()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President");
        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        var service = new VotingService(db.Factory);
        Assert.True(service.CastVote(voterId, candidateId).Success);

        var second = service.CastVote(voterId, candidateId);
        Assert.False(second.Success);
        Assert.Contains("already voted", second.Message, StringComparison.OrdinalIgnoreCase);

        using var context = db.Factory.CreateContext();
        Assert.Equal(1, context.Ballots.Count());
    }

    [Fact]
    public void CastVote_rejects_when_election_is_closed()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President", ElectionStatus.Closed);
        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        var service = new VotingService(db.Factory);
        var result = service.CastVote(voterId, candidateId);

        Assert.False(result.Success);
        Assert.Contains("not currently open", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CastVote_rejects_unverified_voter()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President");
        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100", VoterVerificationStatus.PendingVerification);

        var service = new VotingService(db.Factory);
        var result = service.CastVote(voterId, candidateId);

        Assert.False(result.Success);
        Assert.Contains("not been verified", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CastVote_rejects_when_deadline_passed()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President");
        using (var context = db.Factory.CreateContext())
        {
            var election = context.Elections.Single(e => e.Id == electionId);
            election.ClosesAt = DateTime.UtcNow.AddMinutes(-1);
            context.SaveChanges();
        }

        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        var result = new VotingService(db.Factory).CastVote(voterId, candidateId);
        Assert.False(result.Success);
        Assert.Contains("deadline", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Voting_for_one_election_does_not_affect_another()
    {
        using var db = new TestDatabase();
        int openElection = db.AddElection("Open Election", "President");
        int closedElection = db.AddElection("Closed Election", "President", ElectionStatus.Closed);
        int openCandidate = db.AddCandidate(openElection, "Alice", "President");
        int closedCandidate = db.AddCandidate(closedElection, "Carol", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        var service = new VotingService(db.Factory);

        Assert.True(service.CastVote(voterId, openCandidate).Success);
        Assert.False(service.CastVote(voterId, closedCandidate).Success);

        using var context = db.Factory.CreateContext();
        Assert.Equal(1, context.Ballots.Count(b => b.ElectionId == openElection));
        Assert.Equal(0, context.Ballots.Count(b => b.ElectionId == closedElection));
    }

    [Fact]
    public void Ballot_has_no_voter_link_separating_identity_from_choice()
    {
        using var db = new TestDatabase();
        int electionId = db.AddElection("Guild", "President");
        int candidateId = db.AddCandidate(electionId, "Alice", "President");
        int voterId = db.AddVoter("Bob", "bob", "V100");

        new VotingService(db.Factory).CastVote(voterId, candidateId);

        using var context = db.Factory.CreateContext();
        var ballot = context.Ballots.Single();
        var receipt = context.VoteReceipts.Single();

        Assert.Equal(candidateId, ballot.CandidateId);
        Assert.Equal(voterId, receipt.VoterId);
        // Distinct identities: a ballot carries no voter reference at all.
        Assert.Null(typeof(Ballot).GetProperty("VoterId"));
    }
}
