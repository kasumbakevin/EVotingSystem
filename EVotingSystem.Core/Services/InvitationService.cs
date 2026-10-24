using System.Security.Cryptography;
using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Generates per-voter one-time invitation links and dispatches them through the
    /// configured <see cref="IInvitationSender"/>. When no provider is configured the
    /// sender exports the links only and reports that nothing was sent.
    /// </summary>
    public sealed class InvitationService
    {
        private readonly DatabaseFactory _databaseFactory;
        private readonly IInvitationSender _sender;
        private readonly string _baseUrl;

        public InvitationService(DatabaseFactory databaseFactory, IInvitationSender sender, string? baseUrl = null)
        {
            _databaseFactory = databaseFactory;
            _sender = sender;
            _baseUrl = (baseUrl ?? "http://localhost:5024").TrimEnd('/');
        }

        public bool IsProviderConfigured => _sender.IsConfigured;

        public InvitationDispatchResult SendInvitations(IReadOnlyList<int> voterIds, TimeSpan? validFor = null)
        {
            using var context = _databaseFactory.CreateContext();
            var lifetime = validFor ?? TimeSpan.FromDays(14);
            var requests = new List<InvitationRequest>();

            foreach (var voterId in voterIds.Distinct())
            {
                var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
                if (voter == null)
                {
                    continue;
                }

                var token = voter.InvitationToken;
                if (string.IsNullOrWhiteSpace(token)
                    || voter.InvitationExpiresAt == null
                    || voter.InvitationExpiresAt <= DateTime.UtcNow)
                {
                    token = GenerateToken();
                    voter.InvitationToken = token;
                    voter.InvitationExpiresAt = DateTime.UtcNow.Add(lifetime);
                }

                requests.Add(new InvitationRequest(
                    voter.Name,
                    voter.Email,
                    voter.PhoneNumber,
                    BuildInvitationUrl(token)));
            }

            var result = _sender.Send(requests);

            if (result.ProviderConfigured)
            {
                var dispatchedAt = DateTime.UtcNow;
                foreach (var voterId in voterIds.Distinct())
                {
                    var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
                    if (voter != null)
                    {
                        voter.InvitationSentAt = dispatchedAt;
                    }
                }
            }

            context.SaveChanges();
            return result;
        }

        public string BuildInvitationUrl(string token) => $"{_baseUrl}/invite/{token}";

        public Voter? ResolveInvitation(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.InvitationToken == token);
            if (voter == null || voter.InvitationExpiresAt == null || voter.InvitationExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            return voter;
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(24);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
