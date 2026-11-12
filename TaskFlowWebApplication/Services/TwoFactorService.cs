using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly AppDbContext _context;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _configuration;

        private const int ChallengeExpirationMinutes = 5;
        private const int MaxFailedAttempts = 5;

        public TwoFactorService(
            AppDbContext context,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration configuration)
        {
            _context = context;

            _protector = dataProtectionProvider.CreateProtector(
                "TaskFlow.TwoFactor.SecretKey.v1");

            _configuration = configuration;
        }

        //SETUP 2FA
        public async Task<TwoFactorSetupResponseDTO> SetupAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                throw new InvalidOperationException("User không tồn tại.");

            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (twoFactor?.IsEnabled == true)
                throw new InvalidOperationException("2FA đã được bật.");

            //tạo Secret Key ngẫu nhiên 160 bit
            var secretKey = KeyGeneration.GenerateRandomKey(20);

            var secretBase32 = Base32Encoding.ToString(secretKey);

            //mã hóa Secret Key trước khi lưu Database
            var protectedSecret = _protector.Protect(secretBase32);

            if (twoFactor == null)
            {
                twoFactor = new UserTwoFactor
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsEnabled = false
                };

                _context.UserTwoFactors.Add(twoFactor);
            }

            //lưu Secret Key tạm thời
            twoFactor.PendingSecretKeyProtected = protectedSecret;
            twoFactor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            //tạo URI cho Google Authenticator
            var issuer = _configuration["TwoFactor:Issuer"]
                ?? "TaskFlow";

            var accountName = Uri.EscapeDataString(user.Email);
            var encodedIssuer = Uri.EscapeDataString(issuer);

            var otpauthUri =
                $"otpauth://totp/{encodedIssuer}:{accountName}" +
                $"?secret={secretBase32}" +
                $"&issuer={encodedIssuer}" +
                "&digits=6&period=30";

            //tạo QR Code PNG
            using var qrGenerator = new QRCodeGenerator();

            using var qrCodeData = qrGenerator.CreateQrCode(
                otpauthUri,
                QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData);

            var qrBytes = qrCode.GetGraphic(20);

            return new TwoFactorSetupResponseDTO
            {
                SecretKey = secretBase32,
                ManualEntryKey = secretBase32,
                QrCodeBase64 = Convert.ToBase64String(qrBytes)
            };
        }

        //VERIFY SETUP - XÁC NHẬN BẬT 2FA
        public async Task<bool> VerifySetupAsync(
            int userId,
            string code)
        {
            if (!IsValidOtp(code))
                return false;

            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (twoFactor == null ||
                string.IsNullOrWhiteSpace(
                    twoFactor.PendingSecretKeyProtected))
            {
                return false;
            }

            //giải mã Secret Key tạm thời
            var secretBase32 = _protector.Unprotect(
                twoFactor.PendingSecretKeyProtected);

            var secretKey = Base32Encoding.ToBytes(secretBase32);

            var totp = new Totp(secretKey);

            //cho phép sai lệch thời gian nhỏ
            var isValid = totp.VerifyTotp(
                code,
                out long matchedStep,
                new VerificationWindow(previous: 1, future: 1));

            if (!isValid)
                return false;

            //chuyển Secret Key tạm thời thành chính thức
            twoFactor.SecretKeyProtected =
                twoFactor.PendingSecretKeyProtected;

            twoFactor.PendingSecretKeyProtected = null;

            twoFactor.IsEnabled = true;

            twoFactor.EnabledAt = DateTime.UtcNow;

            twoFactor.UpdatedAt = DateTime.UtcNow;

            twoFactor.LastUsedTotpStep = matchedStep;

            await _context.SaveChangesAsync();

            return true;
        }

        //DISABLE 2FA

        public async Task<bool> DisableAsync(
            int userId,
            string password,
            string code)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                !IsValidOtp(code))
            {
                return false;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                return false;

            //xác minh mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash))
            {
                return false;
            }

            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (twoFactor == null ||
                !twoFactor.IsEnabled ||
                string.IsNullOrWhiteSpace(
                    twoFactor.SecretKeyProtected))
            {
                return false;
            }

            //giải mã Secret Key
            var secretBase32 = _protector.Unprotect(
                twoFactor.SecretKeyProtected);

            var secretKey = Base32Encoding.ToBytes(secretBase32);

            var totp = new Totp(secretKey);

            var isValid = totp.VerifyTotp(
                code,
                out _,
                new VerificationWindow(previous: 1, future: 1));

            if (!isValid)
                return false;

            //tắt 2FA và xóa Secret Key
            twoFactor.IsEnabled = false;

            twoFactor.SecretKeyProtected = null;

            twoFactor.PendingSecretKeyProtected = null;

            twoFactor.EnabledAt = null;

            twoFactor.UpdatedAt = DateTime.UtcNow;

            //vô hiệu hóa các Challenge chưa sử dụng
            var activeChallenges = await _context.TwoFactorLoginChallenges
                .Where(x =>
                    x.UserId == userId &&
                    x.UsedAt == null)
                .ToListAsync();

            foreach (var challenge in activeChallenges)
            {
                challenge.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return true;
        }


        //CREATE LOGIN CHALLENGE TOKEN
        public async Task<string> CreateLoginChallengeAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                throw new InvalidOperationException("User không tồn tại.");

            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (twoFactor?.IsEnabled != true)
                throw new InvalidOperationException("2FA chưa được bật.");

            var now = DateTime.UtcNow;

            //vô hiệu hóa Challenge cũ chưa sử dụng
            var oldChallenges = await _context.TwoFactorLoginChallenges
                .Where(x =>
                    x.UserId == userId &&
                    x.UsedAt == null)
                .ToListAsync();

            foreach (var old in oldChallenges)
            {
                old.UsedAt = now;
            }

            //tạo token ngẫu nhiên 32 bytes
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            var challengeToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            //chỉ lưu SHA256 Hash trong Database
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(challengeToken)));

            var challenge = new TwoFactorLoginChallenge
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = now.AddMinutes(
                    ChallengeExpirationMinutes),
                CreatedAt = now,
                FailedAttempts = 0
            };

            _context.TwoFactorLoginChallenges.Add(challenge);

            await _context.SaveChangesAsync();

            //chỉ trả token gốc cho Client
            return challengeToken;
        }

        //VERIFY LOGIN CHALLENGE AND OTP
        public async Task<int?> VerifyLoginAsync(
            string challengeToken,
            string code)
        {
            if (string.IsNullOrWhiteSpace(challengeToken) ||
                !IsValidOtp(code))
            {
                return null;
            }

            //hash Challenge Token
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(challengeToken)));

            //serializable giúp bảo vệ thao tác xác minh
            //trước các request đồng thời trên MySQL/InnoDB.
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var challenge = await _context.TwoFactorLoginChallenges
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            var now = DateTime.UtcNow;

            //kiểm tra Challenge
            if (challenge == null ||
                challenge.UsedAt != null ||
                challenge.ExpiresAt <= now ||
                challenge.FailedAttempts >= MaxFailedAttempts)
            {
                return null;
            }

            //tìm cấu hình 2FA
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x =>
                    x.UserId == challenge.UserId);

            if (twoFactor == null ||
                !twoFactor.IsEnabled ||
                string.IsNullOrWhiteSpace(
                    twoFactor.SecretKeyProtected))
            {
                challenge.UsedAt = now;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return null;
            }

            //giải mã Secret Key
            var secretBase32 = _protector.Unprotect(
                twoFactor.SecretKeyProtected);

            var secretKey = Base32Encoding.ToBytes(secretBase32);

            var totp = new Totp(secretKey);


            //xác minh OTP và lấy thời điểm OTP tương ứng
            var isValid = totp.VerifyTotp(
                code,
                out long matchedStep,
                new VerificationWindow(previous: 1, future: 1));

            //kiểm tra OTP có hợp lệ và đã từng được sử dụng chưa
            if (!isValid ||
                (twoFactor.LastUsedTotpStep.HasValue &&
                 matchedStep <= twoFactor.LastUsedTotpStep.Value))
            {
                challenge.FailedAttempts++;

                if (challenge.FailedAttempts >= MaxFailedAttempts)
                    challenge.UsedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return null;
            }

            //đánh dấu OTP đã được sử dụng
            twoFactor.LastUsedTotpStep = matchedStep;

            //đánh dấu Challenge đã sử dụng
            challenge.UsedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return challenge.UserId;


            //đánh dấu Challenge đã sử dụng
            challenge.UsedAt = now;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            //trả về UserId nếu OTP hợp lệ
            return challenge.UserId;
        }

        //VALIDATE OTP FORMAT
        private static bool IsValidOtp(string? code)
        {
            return !string.IsNullOrWhiteSpace(code) &&
                   Regex.IsMatch(code, @"^\d{6}$");
        }
    }
}
