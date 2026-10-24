namespace EVotingSystem.Models
{
    // The secret ballot itself: the chosen candidate only.
    // Deliberately has NO voter foreign key so that a ballot cannot be linked
    // back to the voter who cast it.
    public class Ballot
    {
        public int Id { get; set; }
        public int ElectionId { get; set; }
        public int CandidateId { get; set; }
        public string Position { get; set; } = string.Empty;
        public DateTime CastAt { get; set; } = DateTime.UtcNow;

        public Ballot() { }

        public Ballot(int electionId, int candidateId, string position)
        {
            ElectionId = electionId;
            CandidateId = candidateId;
            Position = position;
        }
    }
}
