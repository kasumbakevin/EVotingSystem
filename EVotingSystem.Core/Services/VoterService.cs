using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Services
{
    /// <summary>
    /// Voter register operations: registration, editing, searching, verification,
    /// deactivation and reinstatement. Self-registration does not by itself
    /// establish eligibility; verification is a separate, deliberate step.
    /// </summary>
    public class VoterService
    {
        private readonly DatabaseFactory _databaseFactory;

        public VoterService(DatabaseFactory databaseFactory)
        {
            _databaseFactory = databaseFactory;
        }

        public IReadOnlyList<Voter> GetAll()
        {
            using var context = _databaseFactory.CreateContext();
            return context.Voters.OrderBy(v => v.Id).ToList();
        }

        public IReadOnlyList<Voter> Search(string? term)
        {
            using var context = _databaseFactory.CreateContext();
            var query = context.Voters.AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                query = query.Where(v =>
                    v.Name.Contains(t) ||
                    v.Username.Contains(t) ||
                    v.VoterNumber.Contains(t) ||
                    (v.Department != null && v.Department.Contains(t)));
            }

            return query.OrderBy(v => v.Id).ToList();
        }

        public ServiceResult Register(
            string fullName,
            string username,
            string password,
            string voterNumber,
            string? department = null,
            string? email = null,
            string? phone = null)
        {
            using var context = _databaseFactory.CreateContext();

            if (string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password)
                || string.IsNullOrWhiteSpace(voterNumber))
            {
                return ServiceResult.Fail("Full name, username, password and voter number are required.");
            }

            if (context.Voters.Any(v => v.Username == username))
            {
                return ServiceResult.Fail("That username is already registered.");
            }

            if (context.Voters.Any(v => v.VoterNumber == voterNumber))
            {
                return ServiceResult.Fail("That voter number is already registered.");
            }

            var voter = new Voter(0, fullName, username, string.Empty, voterNumber)
            {
                Department = department,
                Email = email,
                PhoneNumber = phone,
                VerificationStatus = VoterVerificationStatus.PendingVerification,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow,
            };
            voter.PasswordHash = SeedData.HashPassword(voter, password);
            context.Voters.Add(voter);
            context.SaveChanges();

            return ServiceResult.Ok($"Voter '{fullName}' registered. Status: Pending Verification.");
        }

        public ServiceResult Update(int voterId, string fullName, string username, string voterNumber, string? department, string? email, string? phone)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(voterNumber))
            {
                return ServiceResult.Fail("Full name, username and voter number are required.");
            }

            if (context.Voters.Any(v => v.Id != voterId && v.Username == username))
            {
                return ServiceResult.Fail("That username is already in use.");
            }

            if (context.Voters.Any(v => v.Id != voterId && v.VoterNumber == voterNumber))
            {
                return ServiceResult.Fail("That voter number is already in use.");
            }

            voter.Name = fullName;
            voter.Username = username;
            voter.VoterNumber = voterNumber;
            voter.Department = department;
            voter.Email = email;
            voter.PhoneNumber = phone;
            context.SaveChanges();

            return ServiceResult.Ok($"Voter '{fullName}' updated.");
        }

        public ServiceResult SetReferencePhoto(int voterId, string photoPath)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            voter.PhotoPath = photoPath;
            context.SaveChanges();
            return ServiceResult.Ok("Reference photograph saved.");
        }

        public ServiceResult SetVerification(int voterId, VoterVerificationStatus status)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            voter.VerificationStatus = status;
            context.SaveChanges();
            return ServiceResult.Ok($"Voter '{voter.Name}' marked as {status}.");
        }

        public ServiceResult SetActive(int voterId, bool active)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            voter.IsActive = active;
            context.SaveChanges();
            return ServiceResult.Ok($"Voter '{voter.Name}' {(active ? "reinstated" : "deactivated")}.");
        }

        public ServiceResult ResetPassword(int voterId, string newPassword)
        {
            using var context = _databaseFactory.CreateContext();
            var voter = context.Voters.FirstOrDefault(v => v.Id == voterId);
            if (voter == null)
            {
                return ServiceResult.Fail("Voter not found.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                return ServiceResult.Fail("Password must be at least 8 characters.");
            }

            voter.PasswordHash = SeedData.HashPassword(voter, newPassword);
            context.SaveChanges();
            return ServiceResult.Ok($"Password reset for '{voter.Name}'.");
        }
    }
}
