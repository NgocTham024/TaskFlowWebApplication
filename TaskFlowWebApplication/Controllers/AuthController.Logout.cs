using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Lấy Refresh Token từ HttpOnly Cookie
            if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken)
                && !string.IsNullOrWhiteSpace(refreshToken))
            {
                // Hash token giống cách Login lưu trong database
                var tokenHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

                // Tìm user đang giữ Refresh Token này
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == tokenHash);

                if (user != null)
                {
                    // Thu hồi Refresh Token
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;

                    await _context.SaveChangesAsync();
                }
            }

            // Xóa cookie Refresh Token trên trình duyệt
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            return Ok(new
            {
                message = "Đăng xuất thành công."
            });
        }
    }
}
