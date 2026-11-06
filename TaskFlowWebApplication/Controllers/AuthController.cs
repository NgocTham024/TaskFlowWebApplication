using Microsoft.AspNetCore.Mvc;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Services;
using Microsoft.AspNetCore.RateLimiting;

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
        private readonly IPasswordResetService _passwordResetService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ISocialAuthService _socialAuthService;
        public AuthController(
            AppDbContext context,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            IAuthService authService,
            IPasswordResetService passwordResetService,
            ITwoFactorService twoFactorService,
            ISocialAuthService socialAuthService)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _authService = authService;
            _passwordResetService = passwordResetService;
            _twoFactorService = twoFactorService;
            _socialAuthService = socialAuthService;
        }
    }
}
