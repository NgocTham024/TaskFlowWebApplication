using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskFlowWebApplication.Controller
{
    public partial class AdminController
    {
        // GET api/admin/users/{id}/sessions
        [HttpGet("users/{id}/sessions")]
        public async Task<IActionResult> GetUserSessions(int id)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.UserId == id);

            if (!userExists)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            var sessions = await _context.RefreshTokens
                .AsNoTracking()
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .OrderByDescending(rt => rt.ExpiresAt)
                .Select(rt => new
                {
                    rt.RefreshTokenId,
                    rt.ExpiresAt,
                    isExpired = rt.ExpiresAt <= DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(new
            {
                userId = id,
                activeSessions = sessions.Count,
                sessions
            });
        }

        // DELETE api/admin/users/{id}/sessions
        [HttpDelete("users/{id}/sessions")]
        public async Task<IActionResult> ForceLogout(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            // revoke tất cả refresh tokens trong bảng RefreshTokens
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            // xóa refresh token trên bảng Users
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Force logout thành công.",
                revokedSessions = activeTokens.Count
            });
        }
    }
}
