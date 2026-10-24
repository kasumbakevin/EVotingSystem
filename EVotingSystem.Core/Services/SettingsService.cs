using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Loads and persists single-row application settings such as SMTP configuration.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly DatabaseFactory _databaseFactory;

        public SettingsService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        public EmailSettings GetEmailSettings()
        {
            using var context = _databaseFactory.CreateContext();
            return context.EmailSettings.FirstOrDefault() ?? new EmailSettings();
        }

        public void SaveEmailSettings(EmailSettings settings)
        {
            using var context = _databaseFactory.CreateContext();
            var existing = context.EmailSettings.FirstOrDefault();
            if (existing == null)
            {
                context.EmailSettings.Add(settings);
            }
            else
            {
                existing.Enabled = settings.Enabled;
                existing.SmtpHost = settings.SmtpHost;
                existing.SmtpPort = settings.SmtpPort;
                existing.UseStartTls = settings.UseStartTls;
                existing.Username = settings.Username;
                existing.Password = settings.Password;
                existing.FromAddress = settings.FromAddress;
                existing.FromName = settings.FromName;
                existing.PublicBaseUrl = settings.PublicBaseUrl;
            }

            context.SaveChanges();
        }
    }
}
