using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;
        private readonly UserRoleService _userRoleService;
        public AuthController(
            AppDbContext context,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            IAuthService authService,
            UserRoleService userRoleService)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _authService = authService;
            _userRoleService = userRoleService;
        }
    }
}
