using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDTO dto)
        {
            var result = await _passwordResetService
                .ResetPasswordAsync(dto.Token, dto.NewPassword);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Reset Token không hợp lệ, đã sử dụng hoặc hết hạn."
                });
            }

            return Ok(new
            {
                message = "Đặt lại mật khẩu thành công."
            });
        }
    }
}