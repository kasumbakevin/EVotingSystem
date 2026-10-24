using EVotingSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace EVotingSystem.Data
{
    /// <summary>
    /// Seeds the minimum data required for first-run. Only inserts when a table is
    /// empty, so real election data is never overwritten or deleted.
    /// </summary>
    public static class SeedData
    {
        // Default first-run administrator credentials. The account is flagged with
        // MustChangePassword so it cannot be used for a real election as-is.
        public const string DefaultAdminUsername = "admin";
        public const string DefaultAdminPassword = "ChangeMe123!";

        private static readonly PasswordHasher<Admin> Hasher = new();

        public static void Apply(AppDbContext context)
        {
            if (!context.Admins.Any())
            {
                var admin = new Admin(0, "System Administrator", DefaultAdminUsername, string.Empty)
                {
                    MustChangePassword = true,
                    IsActive = true,
                };
                admin.PasswordHash = Hasher.HashPassword(admin, DefaultAdminPassword);
                context.Admins.Add(admin);
            }

            context.SaveChanges();
        }

        public static string HashPassword(User user, string password)
        {
            var hasher = new PasswordHasher<User>();
            return hasher.HashPassword(user, password);
        }

        public static bool VerifyPassword(User user, string password)
        {
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
