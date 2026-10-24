using EVotingSystem.Data;
using EVotingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Authenticates administrators and voters against hashed credentials stored in
    /// the database. Plaintext passwords are never stored or compared.
    /// </summary>
    public class AuthenticationService
    {
        private readonly DatabaseFactory _databaseFactory;

        public AuthenticationService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        public Admin? AdminLogin(string username, string password)
        {
            using var context = _databaseFactory.CreateContext();
            var admin = context.Admins.FirstOrDefault(a =>
                a.Username == username && a.IsActive);

            if (admin == null)
            {
                return null;
            }

            return SeedData.VerifyPassword(admin, password) ? admin : null;
        }

        public Voter? VoterLogin(string username, string password)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v =>
                v.Username == username && v.IsActive);

            if (voter == null)
            {
                return null;
            }

            return SeedData.VerifyPassword(voter, password) ? voter : null;
        }

        public ServiceResult ChangeAdminPassword(int adminId, string currentPassword, string newPassword)
        {
            using var context = _databaseFactory.CreateContext();
            var admin = context.Admins.FirstOrDefault(a => a.Id == adminId);

            if (admin == null)
            {
                return ServiceResult.Fail("Administrator account not found.");
            }

            if (!SeedData.VerifyPassword(admin, currentPassword))
            {
                return ServiceResult.Fail("Current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                return ServiceResult.Fail("New password must be at least 8 characters.");
            }

            admin.PasswordHash = SeedData.HashPassword(admin, newPassword);
            admin.MustChangePassword = false;
            context.SaveChanges();
            return ServiceResult.Ok("Password updated.");
        }
    }
}
