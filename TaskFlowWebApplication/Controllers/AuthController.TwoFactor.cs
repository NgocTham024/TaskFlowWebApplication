using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        //thêm ITwoFactorService vào constructor hiện tại
        //của AuthController và gán:
        //_twoFactorService = twoFactorService;

        private int GetCurrentUserId()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out var id))
                throw new UnauthorizedAccessException();

            return id;
        }

        [Authorize]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var result = await _twoFactorService.SetupAsync(
                GetCurrentUserId());

            return Ok(result);
        }

        [Authorize]
        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactor(
            [FromBody] VerifyTwoFactorDTO dto)
        {
            var result = await _twoFactorService.VerifySetupAsync(
                GetCurrentUserId(),
                dto.Code);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Mã OTP không hợp lệ."
                });
            }

            return Ok(new
            {
                message = "Đã kích hoạt xác thực hai yếu tố thành công."
            });
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor(
            [FromBody] DisableTwoFactorDTO dto)
        {
            var result = await _twoFactorService.DisableAsync(
                GetCurrentUserId(),
                dto.Password,
                dto.Code);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Mật khẩu hoặc mã OTP không chính xác."
                });
            }

            return Ok(new
            {
                message = "Đã tắt xác thực hai yếu tố."
            });
        }
    }
}
