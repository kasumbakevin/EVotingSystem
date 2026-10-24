using EVotingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EVotingSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Voter> Voters => Set<Voter>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<Election> Elections => Set<Election>();
        public DbSet<Ballot> Ballots => Set<Ballot>();
        public DbSet<VoteReceipt> VoteReceipts => Set<VoteReceipt>();
        public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.ToTable("Admins");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(a => a.Username).IsUnique();
                entity.Property(a => a.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<Voter>(entity =>
            {
                entity.ToTable("Voters");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Name).IsRequired().HasMaxLength(200);
                entity.Property(v => v.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(v => v.Username).IsUnique();
                entity.Property(v => v.PasswordHash).IsRequired();
                entity.Property(v => v.VoterNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(v => v.VoterNumber).IsUnique();
                entity.Property(v => v.VerificationStatus).HasConversion<int>();
            });

            modelBuilder.Entity<Election>(entity =>
            {
                entity.ToTable("Elections");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Ignore(e => e.IsActive);
            });

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.ToTable("Candidates");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Position).IsRequired().HasMaxLength(150);
                entity.Property(c => c.Party).IsRequired().HasMaxLength(150);
                entity.HasOne<Election>()
                    .WithMany()
                    .HasForeignKey(c => c.ElectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Ballot>(entity =>
            {
                entity.ToTable("Ballots");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Position).IsRequired().HasMaxLength(150);
                entity.HasIndex(b => new { b.ElectionId, b.CandidateId });
                entity.HasOne<Election>()
                    .WithMany()
                    .HasForeignKey(b => b.ElectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Candidate>()
                    .WithMany()
                    .HasForeignKey(b => b.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VoteReceipt>(entity =>
            {
                entity.ToTable("VoteReceipts");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Position).IsRequired().HasMaxLength(150);
                entity.HasIndex(r => new { r.ElectionId, r.VoterId, r.Position }).IsUnique();
                entity.HasOne<Election>()
                    .WithMany()
                    .HasForeignKey(r => r.ElectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Voter>()
                    .WithMany()
                    .HasForeignKey(r => r.VoterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmailSettings>(entity =>
            {
                entity.ToTable("EmailSettings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SmtpHost).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.FromAddress).HasMaxLength(255);
                entity.Property(e => e.FromName).HasMaxLength(200);
                entity.Property(e => e.PublicBaseUrl).HasMaxLength(500);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Category).IsRequired().HasMaxLength(50);
                entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
                entity.Property(a => a.Details).HasMaxLength(1000);
                entity.Property(a => a.ActorRole).HasMaxLength(50);
                entity.HasIndex(a => a.TimestampUtc);
            });
        }
    }
}
