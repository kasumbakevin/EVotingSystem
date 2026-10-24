using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Casts votes. A ballot is only accepted when the target election is Open and
    /// the voter is eligible. The participation record (<see cref="VoteReceipt"/>)
    /// and the secret choice (<see cref="Ballot"/>) are written in one transaction
    /// and are deliberately unlinkable.
    /// </summary>
    public class VotingService
    {
        private readonly DatabaseFactory _databaseFactory;

        public VotingService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        public ServiceResult CastVote(int voterId, int candidateId)
        {
            using var context = _databaseFactory.CreateContext();
            using var transaction = context.Database.BeginTransaction();

            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            if (!voter.IsActive)
            {
                return ServiceResult.Fail("This voter account is not active.");
            }

            if (voter.VerificationStatus != VoterVerificationStatus.Verified)
            {
                return ServiceResult.Fail("Voter identity has not been verified.");
            }

            var candidate = context.Candidates.FirstOrDefault(c => c.Id == candidateId);
            if (candidate == null)
            {
                return ServiceResult.Fail("Candidate not found.");
            }

            // Only the election the candidate belongs to is considered. A closed or
            // differently-scoped election can never accept this vote.
            var election = context.Elections.FirstOrDefault(e => e.Id == candidate.ElectionId);
            if (election == null)
            {
                return ServiceResult.Fail("The election for this candidate no longer exists.");
            }

            if (election.Status != ElectionStatus.Open)
            {
                return ServiceResult.Fail("The election is not currently open for voting.");
            }

            var now = DateTime.UtcNow;
            if (election.ClosesAt.HasValue && election.ClosesAt.Value <= now)
            {
                return ServiceResult.Fail("The voting deadline has passed.");
            }

            var alreadyVoted = context.VoteReceipts.Any(r =>
                r.ElectionId == election.Id
                && r.VoterId == voterId
                && r.Position == candidate.Position);
            if (alreadyVoted)
            {
                return ServiceResult.Fail("You have already voted for this position.");
            }

            // Participation record and secret ballot are separate rows with no link.
            context.VoteReceipts.Add(new VoteReceipt(election.Id, voterId, candidate.Position));
            context.Ballots.Add(new Ballot(election.Id, candidate.Id, candidate.Position));

            try
            {
                context.SaveChanges();
                transaction.Commit();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Unique constraint on (ElectionId, VoterId, Position) caught a
                // duplicate that raced past the check above.
                transaction.Rollback();
                return ServiceResult.Fail("You have already voted for this position.");
            }

            bool voterComplete = context.VoteReceipts
                .Where(r => r.ElectionId == election.Id && r.VoterId == voterId)
                .Select(r => r.Position)
                .Distinct()
                .Count()
                >= context.Candidates
                    .Where(c => c.ElectionId == election.Id)
                    .Select(c => c.Position)
                    .Distinct()
                    .Count();

            if (voterComplete)
            {
                voter.HasVoted = true;
                context.SaveChanges();
            }

            return ServiceResult.Ok(
                "Vote cast successfully. Your ballot has been recorded confidentially.");
        }
    }
}
