
using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin(
            [FromBody] GoogleLoginDTO dto)
        {
            // 1. Xác minh Google ID Token
            var result = await _socialAuthService.GoogleLoginAsync(
                dto.IdToken);

            if (!result.Success || result.UserId == null)
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            // 2. Nếu tài khoản bật 2FA
            if (result.RequiresTwoFactor)
            {
                return Ok(new
                {
                    requiresTwoFactor = true,
                    challengeToken = result.ChallengeToken,
                    message = result.Message
                });
            }

            // 3. Lấy User để cấp JWT
            var user = await _context.Users.FindAsync(result.UserId.Value);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Không tìm thấy tài khoản TaskFlow."
                });
            }

            // 4. Cấp Access Token và Refresh Token
            return await CompleteLoginAsync(user);
        }
    }
}
