using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controller
{
    public partial class AdminController
    {
        // GET api/admin/users?page=1&pageSize=10&search=keyword
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            // tìm kiếm theo username hoặc email
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.Username.Contains(search) ||
                    u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.Phone,
                    u.IsEmailVerified
                })
                .ToListAsync();

            return Ok(new
            {
                data = users,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        // GET api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            // lấy roles của user
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.RoleId
                where userRole.UserId == id
                select new { role.RoleId, role.RoleName }
            ).ToListAsync();

            return Ok(new
            {
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.Phone,
                    user.IsEmailVerified
                },
                roles
            });
        }

        // PUT api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] AdminUpdateUserDTO dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            // kiểm tra username trùng
            if (!string.IsNullOrWhiteSpace(dto.Username)
                && dto.Username != user.Username)
            {
                var usernameExists = await _context.Users
                    .AnyAsync(u => u.Username == dto.Username && u.UserId != id);

                if (usernameExists)
                {
                    return BadRequest(new { message = "Username đã tồn tại." });
                }

                user.Username = dto.Username;
            }

            // kiểm tra email trùng
            if (!string.IsNullOrWhiteSpace(dto.Email)
                && dto.Email != user.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email && u.UserId != id);

                if (emailExists)
                {
                    return BadRequest(new { message = "Email đã tồn tại." });
                }

                user.Email = dto.Email;
            }

            if (dto.Phone != null)
            {
                user.Phone = dto.Phone;
            }

            if (dto.IsEmailVerified.HasValue)
            {
                user.IsEmailVerified = dto.IsEmailVerified.Value;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật user thành công.",
                user = new
                {
                    user.UserId,
                    user.Username,
                    user.Email,
                    user.Phone,
                    user.IsEmailVerified
                }
            });
        }

        // DELETE api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            // xóa roles của user
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .ToListAsync();
            _context.UserRoles.RemoveRange(userRoles);

            // xóa refresh tokens
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id)
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(refreshTokens);

            // xóa password reset tokens
            var resetTokens = await _context.PasswordResetTokens
                .Where(pt => pt.UserId == id)
                .ToListAsync();
            _context.PasswordResetTokens.RemoveRange(resetTokens);

            // xóa social accounts
            var socialAccounts = await _context.SocialAccounts
                .Where(sa => sa.UserId == id)
                .ToListAsync();
            _context.SocialAccounts.RemoveRange(socialAccounts);

            // xóa user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa user thành công." });
        }

        // POST api/admin/users/{id}/verify-email
        [HttpPost("users/{id}/verify-email")]
        public async Task<IActionResult> ManualVerifyEmail(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            if (user.IsEmailVerified)
            {
                return BadRequest(new { message = "Email đã được xác thực trước đó." });
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực email thủ công thành công." });
        }
    }
}
