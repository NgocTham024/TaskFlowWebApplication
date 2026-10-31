using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskFlowWebApplication.Controller
{
    public partial class AdminController
    {
        // GET api/admin/dashboard/stats
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var today = DateTime.UtcNow.Date;

            var totalUsers = await _context.Users.CountAsync();

            var verifiedUsers = await _context.Users
                .CountAsync(u => u.IsEmailVerified);

            var unverifiedUsers = totalUsers - verifiedUsers;

            var totalRoles = await _context.Roles.CountAsync();

            var activeRefreshTokens = await _context.RefreshTokens
                .CountAsync(rt =>
                    rt.RevokedAt == null &&
                    rt.ExpiresAt > DateTime.UtcNow);

            return Ok(new
            {
                totalUsers,
                verifiedUsers,
                unverifiedUsers,
                totalRoles,
                activeRefreshTokens
            });
        }
    }
}
