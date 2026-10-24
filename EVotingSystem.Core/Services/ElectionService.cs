using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Manages the election lifecycle. Opening and closing are guarded transitions:
    /// a closed election cannot silently reopen, and acting on one election never
    /// affects another.
    /// </summary>
    public class ElectionService
    {
        private readonly DatabaseFactory _databaseFactory;

        public ElectionService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        /// <summary>
        /// Validates the election configuration, then opens voting and records who
        /// opened it and when.
        /// </summary>
        public ServiceResult StartElection(int electionId, int adminId)
        {
            using var context = _databaseFactory.CreateContext();
            var election = context.Elections.FirstOrDefault(e => e.Id == electionId);

            if (election == null)
            {
                return ServiceResult.Fail("Election not found.");
            }

            if (election.Status == ElectionStatus.Open)
            {
                return ServiceResult.Fail("This election is already open.");
            }

            if (election.Status == ElectionStatus.Closed
                || election.Status == ElectionStatus.Cancelled
                || election.Status == ElectionStatus.Archived)
            {
                return ServiceResult.Fail($"A {election.Status} election cannot be reopened.");
            }

            var candidateCount = context.Candidates.Count(c => c.ElectionId == electionId);
            if (candidateCount == 0)
            {
                return ServiceResult.Fail("Cannot start an election with no candidates.");
            }

            var eligibleVoterCount = context.Voters.Count(v =>
                v.IsActive && v.VerificationStatus == VoterVerificationStatus.Verified);
            if (eligibleVoterCount == 0)
            {
                return ServiceResult.Fail("No verified, eligible voters are registered.");
            }

            var now = DateTime.UtcNow;
            if (election.OpensAt.HasValue && election.OpensAt.Value > now)
            {
                return ServiceResult.Fail(
                    $"Voting cannot open before the scheduled opening time ({election.OpensAt.Value:u}).");
            }

            if (election.ClosesAt.HasValue && election.ClosesAt.Value <= now)
            {
                return ServiceResult.Fail("The closing time has already passed.");
            }

            election.Status = ElectionStatus.Open;
            election.OpenedAt = now;
            election.OpenedByAdminId = adminId;
            context.SaveChanges();

            return ServiceResult.Ok(
                $"Election '{election.Title}' is now open. {eligibleVoterCount} eligible voter(s), {candidateCount} candidate(s).");
        }

        public ServiceResult PauseElection(int electionId)
        {
            using var context = _databaseFactory.CreateContext();
            var election = context.Elections.FirstOrDefault(e => e.Id == electionId);

            if (election == null)
            {
                return ServiceResult.Fail("Election not found.");
            }

            if (election.Status != ElectionStatus.Open)
            {
                return ServiceResult.Fail("Only an open election can be paused.");
            }

            election.Status = ElectionStatus.Paused;
            context.SaveChanges();
            return ServiceResult.Ok($"Election '{election.Title}' has been paused.");
        }

        public ServiceResult ResumeElection(int electionId)
        {
            using var context = _databaseFactory.CreateContext();
            var election = context.Elections.FirstOrDefault(e => e.Id == electionId);

            if (election == null)
            {
                return ServiceResult.Fail("Election not found.");
            }

            if (election.Status != ElectionStatus.Paused)
            {
                return ServiceResult.Fail("Only a paused election can be resumed.");
            }

            election.Status = ElectionStatus.Open;
            context.SaveChanges();
            return ServiceResult.Ok($"Election '{election.Title}' has resumed.");
        }

        public ServiceResult CloseElection(int electionId)
        {
            using var context = _databaseFactory.CreateContext();
            var election = context.Elections.FirstOrDefault(e => e.Id == electionId);

            if (election == null)
            {
                return ServiceResult.Fail("Election not found.");
            }

            if (election.Status != ElectionStatus.Open && election.Status != ElectionStatus.Paused)
            {
                return ServiceResult.Fail("Only an open or paused election can be closed.");
            }

            election.Status = ElectionStatus.Closed;
            election.ClosedAt = DateTime.UtcNow;
            context.SaveChanges();
            return ServiceResult.Ok($"Election '{election.Title}' has been closed.");
        }

        /// <summary>
        /// Closes every election whose deadline has passed. Intended to be called
        /// periodically by the host so that voting stops even without manual action.
        /// </summary>
        public int CloseExpiredElections()
        {
            using var context = _databaseFactory.CreateContext();
            var now = DateTime.UtcNow;
            var expired = context.Elections
                .Where(e => e.ClosesAt != null
                    && e.ClosesAt <= now
                    && (e.Status == ElectionStatus.Open || e.Status == ElectionStatus.Paused))
                .ToList();

            foreach (var election in expired)
            {
                election.Status = ElectionStatus.Closed;
                election.ClosedAt = now;
            }

            if (expired.Count > 0)
            {
                context.SaveChanges();
            }

            return expired.Count;
        }
    }
}
