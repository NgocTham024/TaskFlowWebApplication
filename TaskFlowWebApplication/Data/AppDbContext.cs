using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Model;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserTwoFactor> UserTwoFactors { get; set; }
        public DbSet<TwoFactorLoginChallenge> TwoFactorLoginChallenges { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserTwoFactor>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            modelBuilder.Entity<UserTwoFactor>()
                .HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<UserTwoFactor>(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TwoFactorLoginChallenge>()
                .HasIndex(x => x.TokenHash)
                .IsUnique();

            modelBuilder.Entity<TwoFactorLoginChallenge>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SOCIAL LOGIN
            modelBuilder.Entity<SocialAccount>()
                .HasIndex(x => new { x.Provider, x.ProviderUserId })
                .IsUnique();

            modelBuilder.Entity<SocialAccount>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
