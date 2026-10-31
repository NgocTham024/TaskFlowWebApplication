using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.Data;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public partial class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
    }
}
