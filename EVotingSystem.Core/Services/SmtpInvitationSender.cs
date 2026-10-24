using System.Net;
using System.Net.Mail;
using System.Text;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Delivers voter invitations over SMTP. When SMTP is not configured it reports
    /// the invitations as unsent rather than pretending they were delivered.
    /// </summary>
    public sealed class SmtpInvitationSender : IInvitationSender
    {
        private readonly EmailSettings _settings;

        public SmtpInvitationSender(EmailSettings settings)
        {
            _settings = settings;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public InvitationDispatchResult Send(IReadOnlyList<InvitationRequest> invitations)
        {
            if (!IsConfigured)
            {
                return new InvitationDispatchResult(
                    0,
                    invitations.Count,
                    false,
                    "SMTP is not configured. Invitations were not sent.");
            }

            if (invitations.Count == 0)
            {
                return new InvitationDispatchResult(0, 0, true, "No invitations to send.");
            }

            var sent = 0;
            var failed = 0;
            var errors = new StringBuilder();

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseStartTls,
            };

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            var from = new MailAddress(
                _settings.FromAddress,
                string.IsNullOrWhiteSpace(_settings.FromName) ? _settings.FromAddress : _settings.FromName);

            foreach (var invitation in invitations)
            {
                if (string.IsNullOrWhiteSpace(invitation.Email))
                {
                    failed++;
                    errors.AppendLine($"{invitation.VoterName}: no email address on file.");
                    continue;
                }

                try
                {
                    using var message = new MailMessage(from, new MailAddress(invitation.Email))
                    {
                        Subject = "Your voting invitation",
                        Body = BuildBody(invitation),
                        IsBodyHtml = true,
                    };
                    client.Send(message);
                    sent++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.AppendLine($"{invitation.VoterName}: {ex.Message}");
                }
            }

            var message2 = failed == 0
                ? $"Sent {sent} invitation(s)."
                : $"Sent {sent}, failed {failed}.{Environment.NewLine}{errors}";

            return new InvitationDispatchResult(sent, failed, true, message2);
        }

        private static string BuildBody(InvitationRequest invitation)
        {
            return $@"<!doctype html><html><body style=""font-family:Segoe UI,Arial,sans-serif;padding:24px"">
<h2>You are invited to vote</h2>
<p>Dear {WebUtility.HtmlEncode(invitation.VoterName)},</p>
<p>You have been invited to participate in an election. Use the secure link below to continue:</p>
<p><a href=""{WebUtility.HtmlEncode(invitation.InvitationUrl)}"">{WebUtility.HtmlEncode(invitation.InvitationUrl)}</a></p>
<p>This link is unique to you and should not be shared. If you did not expect this message, please ignore it.</p>
</body></html>";
        }
    }
}
