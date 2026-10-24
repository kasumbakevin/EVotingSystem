using EVotingSystem.Data;
using EVotingSystem.Models;
using EVotingSystem.Services;

namespace EVotingSystem.Tests;

public class AuthenticationTests
{
    [Fact]
    public void AdminLogin_succeeds_with_seeded_default_password()
    {
        using var db = new TestDatabase();
        var service = new AuthenticationService(db.Factory);

        var admin = service.AdminLogin(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword);

        Assert.NotNull(admin);
        Assert.True(admin!.MustChangePassword);
    }

    [Fact]
    public void AdminLogin_fails_with_plaintext_that_is_not_hashed()
    {
        using var db = new TestDatabase();
        var service = new AuthenticationService(db.Factory);

        var admin = service.AdminLogin(SeedData.DefaultAdminUsername, "not-a-real-password");

        Assert.Null(admin);
    }

    [Fact]
    public void Stored_admin_password_is_hashed_not_plaintext()
    {
        using var db = new TestDatabase();
        using var context = db.Factory.CreateContext();
        var admin = context.Admins.Single();

        Assert.NotEqual(SeedData.DefaultAdminPassword, admin.PasswordHash);
        Assert.DoesNotContain(SeedData.DefaultAdminPassword, admin.PasswordHash);
    }

    [Fact]
    public void ChangePassword_requires_correct_current_password()
    {
        using var db = new TestDatabase();
        var service = new AuthenticationService(db.Factory);
        var admin = service.AdminLogin(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword)!;

        var wrong = service.ChangeAdminPassword(admin.Id, "wrong", "NewPassword123!");
        Assert.False(wrong.Success);

        var right = service.ChangeAdminPassword(admin.Id, SeedData.DefaultAdminPassword, "NewPassword123!");
        Assert.True(right.Success);

        Assert.NotNull(service.AdminLogin(SeedData.DefaultAdminUsername, "NewPassword123!"));
        Assert.Null(service.AdminLogin(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));
    }

    [Fact]
    public void Inactive_voter_cannot_log_in()
    {
        using var db = new TestDatabase();
        int voterId = db.AddVoter("Ada", "ada", "V001");
        using (var context = db.Factory.CreateContext())
        {
            context.Voters.Single(v => v.Id == voterId).IsActive = false;
            context.SaveChanges();
        }

        var service = new AuthenticationService(db.Factory);
        Assert.Null(service.VoterLogin("ada", "Password123!"));
    }
}
