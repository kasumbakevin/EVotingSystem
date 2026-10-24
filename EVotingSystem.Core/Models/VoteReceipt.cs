namespace EVotingSystem.Models
{
    // Records that a voter participated in an election for a given position.
    // Kept deliberately separate from Ballot so that no voter-to-choice mapping
    // is queryable. Unique on (ElectionId, VoterId, Position).
    public class VoteReceipt
    {
        public int Id { get; set; }
        public int ElectionId { get; set; }
        public int VoterId { get; set; }
        public string Position { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public VoteReceipt() { }

        public VoteReceipt(int electionId, int voterId, string position)
        {
            ElectionId = electionId;
            VoterId = voterId;
            Position = position;
        }
    }
}
