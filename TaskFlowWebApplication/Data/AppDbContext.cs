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
        public DbSet<Roles> Roles { get; set; }
        public DbSet<UserRoles> UserRoles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<SocialAccount> SocialAccounts { get; set; }
    }
}
