using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/task-statuses")]
    public class TaskStatusesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TaskStatusesController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetStatuses()
        {
            var statuses = await _context.TaskStatuses.AsNoTracking()
                .Where(s =>s.Name == "Todo" ||s.Name == "Doing" ||s.Name == "Done").Select(s => new TaskStatusResponseDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description
                }).ToListAsync();

            var result = statuses.OrderBy(s => s.Name == "Todo" ? 0 :s.Name == "Doing" ? 1 : 2).ToList();
            return Ok(result);
        }
    }
}
