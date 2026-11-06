using Microsoft.AspNetCore.Mvc;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            // Lấy Refresh Token từ HttpOnly Cookie
            if (!Request.Cookies.TryGetValue(
                    "refreshToken",
                    out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new
                {
                    message = "Không tìm thấy Refresh Token."
                });
            }

            // Gọi AuthService để xác minh và xoay vòng token
            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.Success)
            {
                // Xóa Cookie nếu Refresh Token không hợp lệ
                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            // Cập nhật Cookie bằng Refresh Token mới
            SetRefreshTokenCookie(
                result.RefreshToken!,
                result.RefreshTokenExpirationDays);

            // Trả Access Token mới cho Frontend
            return Ok(new
            {
                accessToken = result.AccessToken,
                expiresAt = result.ExpiresAt
            });
        }

    }
}
