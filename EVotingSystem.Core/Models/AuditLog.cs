namespace EVotingSystem.Models
{
    /// <summary>
    /// A single immutable audit record describing an administrative action. Audit
    /// entries are append-only and never modified or deleted by the application.
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public int? ActorId { get; set; }
        public int? ElectionId { get; set; }
    }

    public static class AuditCategories
    {
        public const string Voter = "Voter";
        public const string Candidate = "Candidate";
        public const string Election = "Election";
        public const string Report = "Report";
        public const string Security = "Security";
        public const string Invitation = "Invitation";
    }
}
