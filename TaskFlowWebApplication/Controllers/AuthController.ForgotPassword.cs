using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDTO dto)
        {
            await _passwordResetService
                .RequestPasswordResetAsync(dto.Email);

            return Ok(new
            {
                message = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi."
            });
        }
    }
}