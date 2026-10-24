namespace EVotingSystem.Models
{
    public enum VoterVerificationStatus
    {
        PendingVerification = 0,
        Verified = 1,
        ManualReview = 2,
        Rejected = 3,
    }

    public class Voter : User
    {
        public string VoterNumber { get; set; } = string.Empty;
        public string? Institution { get; set; }
        public string? Department { get; set; }
        public string? Course { get; set; }
        public string? Constituency { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhotoPath { get; set; }
        public bool ConsentGiven { get; set; }
        public VoterVerificationStatus VerificationStatus { get; set; } = VoterVerificationStatus.PendingVerification;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        public string? InvitationToken { get; set; }
        public DateTime? InvitationExpiresAt { get; set; }
        public DateTime? InvitationSentAt { get; set; }

        public Voter() { }

        public Voter(int id, string name, string username, string passwordHash, string voterNumber)
            : base(id, name, username, passwordHash)
        {
            VoterNumber = voterNumber;
        }

        public bool HasVoted { get; set; }

        public void MarkAsVoted()
        {
            HasVoted = true;
        }

        public override void DisplayRole()
        {
            Console.WriteLine("Role: Voter");
        }
    }
}
