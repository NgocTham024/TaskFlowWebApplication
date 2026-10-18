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
    }
}
