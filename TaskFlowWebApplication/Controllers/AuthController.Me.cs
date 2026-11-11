using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [Authorize]
        [HttpGet("me")]

        public async Task<IActionResult> GetMe()
        {
            // Lấy UserId từ JWT đã được xác thực
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    message = "Token không hợp lệ."
                });
            }

            // Tìm user trong database
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản."
                });
            }

            // Chỉ trả về thông tin cần thiết
            return Ok(new
            {
                userId = user.UserId,
                username = user.Username,
                email = user.Email,
                phone = user.Phone,
                isEmailVerified = user.IsEmailVerified
            });
        }
    }
}