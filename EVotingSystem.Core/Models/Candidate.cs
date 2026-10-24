namespace EVotingSystem.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Party { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public string? Manifesto { get; set; }
        public string? PhotoPath { get; set; }
        public int ElectionId { get; set; }
        public int VoteCount { get; private set; }

        public Candidate() { }

        public Candidate(int id, string name, string position, string party)
        {
            Id = id;
            Name = name;
            Position = position;
            Party = party;
            VoteCount = 0;
        }

        public void AddVote()
        {
            VoteCount++;
        }
    }
}
