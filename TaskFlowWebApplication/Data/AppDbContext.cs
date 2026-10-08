using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Users> Users { get; set; }
    }
}
