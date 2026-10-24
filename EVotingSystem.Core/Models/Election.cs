namespace EVotingSystem.Models
{
    public enum ElectionStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Scheduled = 2,
        Open = 3,
        Paused = 4,
        Closed = 5,
        Cancelled = 6,
        Archived = 7,
    }

    public class Election
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Position { get; set; } = string.Empty;
        public int? Year { get; set; }
        public ElectionStatus Status { get; set; } = ElectionStatus.Draft;
        public DateTime? OpensAt { get; set; }
        public DateTime? ClosesAt { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public int? OpenedByAdminId { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public bool IsActive => Status == ElectionStatus.Open;

        public Election() { }

        public Election(int id, string title, string position)
        {
            Id = id;
            Title = title;
            Position = position;
        }

        public void OpenElection()
        {
            Status = ElectionStatus.Open;
            OpenedAt = DateTime.UtcNow;
        }

        public void CloseElection()
        {
            Status = ElectionStatus.Closed;
            ClosedAt = DateTime.UtcNow;
        }
    }
}
