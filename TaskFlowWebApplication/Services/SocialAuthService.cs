
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialAuthService> _logger;

        public SocialAuthService(
            AppDbContext context,
            ITwoFactorService twoFactorService,
            IConfiguration configuration,
            ILogger<SocialAuthService> logger)
        {
            _context = context;
            _twoFactorService = twoFactorService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SocialLoginResult> GoogleLoginAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return new SocialLoginResult
                {
                    Success = false,
                    Message = "Google ID Token không được để trống."
                };
            }

            // 1. Lấy Google Client ID từ appsettings.json
            var clientId = _configuration[
                "GoogleAuthentication:ClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError(
                    "Google Client ID chưa được cấu hình.");

                return new SocialLoginResult
                {
                    Success = false,
                    Message = "Google Authentication chưa được cấu hình."
                };
            }

            // 2. Xác minh Google ID Token
            GoogleJsonWebSignature.Payload payload;

            try
            {
                var settings =
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { clientId }
                    };

                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    settings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Google ID Token không hợp lệ.");

                return new SocialLoginResult
                {
                    Success = false,
                    Message = "Google ID Token không hợp lệ hoặc đã hết hạn."
                };
            }

            // 3. Kiểm tra thông tin Google
            if (string.IsNullOrWhiteSpace(payload.Subject) ||
                string.IsNullOrWhiteSpace(payload.Email) ||
                !payload.EmailVerified)
            {
                return new SocialLoginResult
                {
                    Success = false,
                    Message = "Tài khoản Google chưa xác minh email hoặc thiếu thông tin."
                };
            }

            var provider = "Google";
            var providerUserId = payload.Subject;
            var email = payload.Email.Trim().ToLowerInvariant();

            // 4. Tìm tài khoản Google đã liên kết
            var socialAccount = await _context.SocialAccounts
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ProviderUserId == providerUserId);

            Users? user;

            if (socialAccount != null)
            {
                // Tài khoản Google đã tồn tại
                user = socialAccount.User;

                if (user == null)
                {
                    return new SocialLoginResult
                    {
                        Success = false,
                        Message = "Không tìm thấy tài khoản TaskFlow liên kết."
                    };
                }
            }
            else
            {
                // 5. Kiểm tra email đã tồn tại trong TaskFlow chưa
                user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (user != null)
                {
                    // Chỉ tự động liên kết nếu email TaskFlow đã xác minh
                    if (!user.IsEmailVerified)
                    {
                        return new SocialLoginResult
                        {
                            Success = false,
                            Message = "Email TaskFlow chưa được xác minh. Vui lòng xác minh email trước."
                        };
                    }
                }
                else
                {
                    // 6. Tạo tài khoản TaskFlow mới
                    var usernameBase = "google_" +
                        providerUserId[..Math.Min(12, providerUserId.Length)];

                    var username = usernameBase;
                    var suffix = 1;

                    while (await _context.Users.AnyAsync(
                        x => x.Username == username))
                    {
                        username = $"{usernameBase}_{suffix++}";
                    }

                    // Tài khoản Google không dùng mật khẩu đăng nhập thông thường
                    var randomPassword =
                        Convert.ToBase64String(
                            System.Security.Cryptography.RandomNumberGenerator
                                .GetBytes(48));

                    user = new Users
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                            randomPassword),
                        IsEmailVerified = true
                    };

                    _context.Users.Add(user);

                    await _context.SaveChangesAsync();
                }

                // 7. Liên kết Google với tài khoản TaskFlow
                var newSocialAccount = new SocialAccount
                {
                    UserId = user.UserId,
                    Provider = provider,
                    ProviderUserId = providerUserId,
                    ProviderEmail = email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SocialAccounts.Add(newSocialAccount);

                await _context.SaveChangesAsync();
            }

            // 8. Kiểm tra tài khoản có bật 2FA không
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (twoFactor?.IsEnabled == true)
            {
                var challengeToken =
                    await _twoFactorService.CreateLoginChallengeAsync(
                        user.UserId);

                return new SocialLoginResult
                {
                    Success = true,
                    Message = "Vui lòng xác minh OTP để hoàn tất đăng nhập.",
                    UserId = user.UserId,
                    RequiresTwoFactor = true,
                    ChallengeToken = challengeToken
                };
            }

            // 9. Không bật 2FA: trả UserId để Controller cấp JWT
            return new SocialLoginResult
            {
                Success = true,
                Message = "Đăng nhập Google thành công.",
                UserId = user.UserId,
                RequiresTwoFactor = false
            };
        }
    }
}
