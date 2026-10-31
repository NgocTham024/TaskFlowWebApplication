using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Controller
{
    public partial class AdminController
    {
        // GET api/admin/roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .Select(r => new
                {
                    r.RoleId,
                    r.RoleName
                })
                .ToListAsync();

            return Ok(new { data = roles });
        }

        // POST api/admin/roles
        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDTO dto)
        {
            // kiểm tra role đã tồn tại
            var exists = await _context.Roles
                .AnyAsync(r => r.RoleName == dto.RoleName);

            if (exists)
            {
                return BadRequest(new { message = "Role đã tồn tại." });
            }

            var role = new Roles
            {
                RoleName = dto.RoleName
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo role thành công.",
                role = new { role.RoleId, role.RoleName }
            });
        }

        // GET api/admin/users/{id}/roles
        [HttpGet("users/{id}/roles")]
        public async Task<IActionResult> GetUserRoles(int id)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.UserId == id);

            if (!userExists)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.RoleId
                where userRole.UserId == id
                select new { role.RoleId, role.RoleName }
            ).ToListAsync();

            return Ok(new { userId = id, roles });
        }

        // POST api/admin/users/{id}/roles
        [HttpPost("users/{id}/roles")]
        public async Task<IActionResult> AssignRole(
            int id,
            [FromBody] AssignRoleDTO dto)
        {
            // kiểm tra user tồn tại
            var userExists = await _context.Users
                .AnyAsync(u => u.UserId == id);

            if (!userExists)
            {
                return NotFound(new { message = "Không tìm thấy user." });
            }

            // kiểm tra role tồn tại
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == dto.RoleId);

            if (role == null)
            {
                return NotFound(new { message = "Không tìm thấy role." });
            }

            // kiểm tra đã gán chưa
            var alreadyAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == id && ur.RoleId == dto.RoleId);

            if (alreadyAssigned)
            {
                return BadRequest(new
                {
                    message = $"User đã được gán role '{role.RoleName}'."
                });
            }

            var userRole = new UserRoles
            {
                UserId = id,
                RoleId = dto.RoleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Gán role '{role.RoleName}' thành công."
            });
        }

        // DELETE api/admin/users/{id}/roles/{roleId}
        [HttpDelete("users/{id}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRole(int id, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur =>
                    ur.UserId == id && ur.RoleId == roleId);

            if (userRole == null)
            {
                return NotFound(new
                {
                    message = "User chưa được gán role này."
                });
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thu hồi role thành công." });
        }
    }
}
