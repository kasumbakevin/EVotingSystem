using EVotingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EVotingSystem.Data
{
    /// <summary>
    /// Creates <see cref="AppDbContext"/> instances backed by a SQLite database.
    /// The database file lives in %LOCALAPPDATA%\EVotingSystem by default so that
    /// real election data is never lost when the application restarts or is rebuilt.
    /// </summary>
    public sealed class DatabaseFactory : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public string DatabasePath { get; }

        public DatabaseFactory(string? databasePath = null)
        {
            DatabasePath = databasePath ?? DefaultDatabasePath();

            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={DatabasePath}")
                .Options;
        }

        public static string DefaultDatabasePath()
        {
            var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDirectory, "EVotingSystem", "evoting.db");
        }

        public AppDbContext CreateContext() => new(_options);

        /// <summary>
        /// Ensures the schema exists and seeds baseline data only when the tables are
        /// empty. Existing election data is never deleted or reset.
        /// </summary>
        public void Initialize()
        {
            using var context = CreateContext();
            context.Database.EnsureCreated();
            ApplySchemaUpgrades(context);
            SeedData.Apply(context);
        }

        /// <summary>
        /// EnsuresCreated() does not evolve an existing database. This adds columns
        /// introduced after the database was first created, without touching data.
        /// </summary>
        private static void ApplySchemaUpgrades(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            connection.Open();

            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(Voters);";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            var upgrades = new (string Column, string Definition)[]
            {
                ("InvitationToken", "TEXT NULL"),
                ("InvitationExpiresAt", "TEXT NULL"),
                ("InvitationSentAt", "TEXT NULL"),
            };

            foreach (var (column, definition) in upgrades)
            {
                if (existingColumns.Contains(column))
                {
                    continue;
                }

                using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE Voters ADD COLUMN {column} {definition};";
                alter.ExecuteNonQuery();
            }

            using (var createSettings = connection.CreateCommand())
            {
                createSettings.CommandText = @"
                    CREATE TABLE IF NOT EXISTS EmailSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Enabled INTEGER NOT NULL,
                        SmtpHost TEXT NULL,
                        SmtpPort INTEGER NOT NULL,
                        UseStartTls INTEGER NOT NULL,
                        Username TEXT NULL,
                        Password TEXT NULL,
                        FromAddress TEXT NULL,
                        FromName TEXT NULL,
                        PublicBaseUrl TEXT NULL
                    );";
                createSettings.ExecuteNonQuery();
            }

            using (var createAudit = connection.CreateCommand())
            {
                createAudit.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AuditLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TimestampUtc TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Action TEXT NOT NULL,
                        Details TEXT NULL,
                        ActorRole TEXT NULL,
                        ActorId INTEGER NULL,
                        ElectionId INTEGER NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_AuditLogs_TimestampUtc ON AuditLogs (TimestampUtc);";
                createAudit.ExecuteNonQuery();
            }

            connection.Close();
        }

        public void Dispose()
        {
        }
    }
}
