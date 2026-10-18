using Microsoft.AspNetCore.Mvc;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            // lấy refresh token từ HttpOnly Cookie
            if (!Request.Cookies.TryGetValue("refreshToken", out string? refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new { message = "Không tìm thấy Refresh Token." });
            }

            // gọi AuthService xử lý logic
            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.Success)
            {
                // xóa cookie nếu token không hợp lệ/hết hạn
                Response.Cookies.Delete("refreshToken");
                return Unauthorized(new { message = result.Message });
            }

            // trả Access Token mới
            return Ok(new
            {
                accessToken = result.AccessToken,
                expiresAt = result.ExpiresAt
            });
        }
    }
}
