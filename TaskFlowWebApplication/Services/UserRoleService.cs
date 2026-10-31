using TaskFlowWebApplication.Data;
using Microsoft.EntityFrameworkCore;
namespace TaskFlowWebApplication.Services
{
    public class UserRoleService
    {
        private readonly AppDbContext _context;

        public UserRoleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetRolesAsync(int userId)
        {
            return await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.RoleId
                where userRole.UserId == userId
                select role.RoleName
            ).Distinct().ToListAsync();
        }
    }
}
