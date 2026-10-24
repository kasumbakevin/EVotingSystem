namespace EVotingSystem.Models
{
    /// <summary>
    /// Persisted SMTP configuration used to deliver voter invitations.
    /// The password is stored locally in the application database; for production
    /// use an OS-protected secret store instead.
    /// </summary>
    public class EmailSettings
    {
        public int Id { get; set; }
        public bool Enabled { get; set; }
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "EVoting System";
        public string PublicBaseUrl { get; set; } = "http://localhost:5024";

        public bool IsConfigured =>
            Enabled
            && !string.IsNullOrWhiteSpace(SmtpHost)
            && SmtpPort > 0
            && !string.IsNullOrWhiteSpace(FromAddress);
    }
}
