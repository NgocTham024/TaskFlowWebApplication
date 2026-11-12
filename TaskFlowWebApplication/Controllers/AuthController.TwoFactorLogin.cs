using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("login/2fa")]
        [EnableRateLimiting("TwoFactorLimit")]
        public async Task<IActionResult> VerifyLoginTwoFactor([FromBody] TwoFactorLoginDTO dto)
        {
            // xác minh Challenge Token và OTP
            var userId = await _twoFactorService.VerifyLoginAsync(
                dto.ChallengeToken,
                dto.Code);

            if (userId == null)
            {
                return Unauthorized(new
                {
                    message = "Challenge Token hoặc mã OTP không hợp lệ, đã hết hạn hoặc đã được sử dụng."
                });
            }

            // lấy User sau khi OTP hợp lệ
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (user == null || user.IsEmailVerified == 0)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản không hợp lệ."
                });
            }

            // kiểm tra 2FA vẫn còn được bật
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == userId.Value);

            if (twoFactor != null && twoFactor.IsEnabled == 0 && user.TwoFactorEnabled == 0)
            {
                return Unauthorized(new
                {
                    message = "2FA không còn được kích hoạt."
                });
            }

            // cấp Access Token và Refresh Token
            return await CompleteLoginAsync(user);
        }
    }
}
