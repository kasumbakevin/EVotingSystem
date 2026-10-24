using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Writes and reads append-only audit records. Callers record a real action
    /// after it has happened; nothing here fabricates activity.
    /// </summary>
    public sealed class AuditService
    {
        private readonly DatabaseFactory _databaseFactory;

        public AuditService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        public void Record(string category, string action, string details, string actorRole, int? actorId = null, int? electionId = null)
        {
            using var context = _databaseFactory.CreateContext();
            context.AuditLogs.Add(new AuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                Category = category,
                Action = action,
                Details = details,
                ActorRole = actorRole,
                ActorId = actorId,
                ElectionId = electionId,
            });
            context.SaveChanges();
        }

        public IReadOnlyList<AuditLog> Recent(int count = 12)
        {
            using var context = _databaseFactory.CreateContext();
            return context.AuditLogs
                .OrderByDescending(a => a.TimestampUtc)
                .ThenByDescending(a => a.Id)
                .Take(count)
                .ToList();
        }

        public IReadOnlyList<AuditLog> All()
        {
            using var context = _databaseFactory.CreateContext();
            return context.AuditLogs
                .OrderByDescending(a => a.TimestampUtc)
                .ThenByDescending(a => a.Id)
                .ToList();
        }
    }
}
