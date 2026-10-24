namespace EVotingSystem.Services
{
    public sealed record InvitationRequest(
        string VoterName,
        string? Email,
        string? PhoneNumber,
        string InvitationUrl);

    public sealed record InvitationDispatchResult(
        int Sent,
        int Failed,
        bool ProviderConfigured,
        string Message,
        string? ExportPath = null);

    /// <summary>
    /// Contract for delivering voter invitations. If no email/SMS provider is
    /// configured, implementations must fall back to a clearly-labelled secure CSV
    /// export for the administrator to distribute manually — never claim that
    /// invitations were sent when they were not.
    /// </summary>
    public interface IInvitationSender
    {
        bool IsConfigured { get; }

        InvitationDispatchResult Send(IReadOnlyList<InvitationRequest> invitations);
    }

    /// <summary>
    /// Fallback used when no messaging provider is configured. Writes an export file
    /// of invitation destinations and reports honestly that nothing was sent.
    /// </summary>
    public sealed class CsvExportInvitationSender : IInvitationSender
    {
        private readonly string _exportDirectory;

        public CsvExportInvitationSender(string exportDirectory)
        {
            _exportDirectory = exportDirectory;
        }

        public bool IsConfigured => false;

        public InvitationDispatchResult Send(IReadOnlyList<InvitationRequest> invitations)
        {
            if (invitations.Count == 0)
            {
                return new InvitationDispatchResult(0, 0, false, "No invitations to export.");
            }

            Directory.CreateDirectory(_exportDirectory);
            var path = Path.Combine(
                _exportDirectory,
                $"invitations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");

            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("VoterName,Email,PhoneNumber,InvitationUrl");
                foreach (var invitation in invitations)
                {
                    writer.WriteLine(string.Join(",",
                        Escape(invitation.VoterName),
                        Escape(invitation.Email),
                        Escape(invitation.PhoneNumber),
                        Escape(invitation.InvitationUrl)));
                }
            }

            return new InvitationDispatchResult(
                0,
                invitations.Count,
                false,
                "No messaging provider is configured. Invitations were exported only and have NOT been sent.",
                path);
        }

        private static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{escaped}\""
                : escaped;
        }
    }
}
