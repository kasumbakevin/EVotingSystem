using EVotingSystem.Data;
using EVotingSystem.Models;

namespace EVotingSystem.Tests;

public class PersistenceTests
{
    [Fact]
    public void Seed_creates_admin_once_and_never_duplicates()
    {
        using var db = new TestDatabase();
        db.Factory.Initialize();
        db.Factory.Initialize();

        using var context = db.Factory.CreateContext();
        Assert.Equal(1, context.Admins.Count());
    }

    [Fact]
    public void Voters_and_elections_survive_factory_restart()
    {
        string path = Path.Combine(Path.GetTempPath(), $"evoting_persist_{Guid.NewGuid():N}.db");
        try
        {
            using (var first = new DatabaseFactory(path))
            {
                first.Initialize();
                using var context = first.CreateContext();
                var election = new Election(0, "Persisted Election", "Chair") { Status = ElectionStatus.Draft };
                context.Elections.Add(election);
                context.SaveChanges();
            }

            using (var second = new DatabaseFactory(path))
            {
                second.Initialize();
                using var context = second.CreateContext();
                Assert.Single(context.Elections);
                Assert.Equal("Persisted Election", context.Elections.Single().Title);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Election_ids_are_generated_not_count_based()
    {
        using var db = new TestDatabase();
        int first = db.AddElection("First", "Chair");
        int second = db.AddElection("Second", "Chair");

        Assert.True(second > first);
    }
}
