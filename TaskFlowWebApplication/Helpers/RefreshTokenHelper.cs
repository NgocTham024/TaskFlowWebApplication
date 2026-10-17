using BCrypt.Net;

namespace TaskFlowWebApplication.Helpers
{
    public class RefreshTokenHelper
    {
        public static string HashToken(string token)
        {
            return BCrypt.Net.BCrypt.HashPassword(token);
        }
        public static bool VerifyToken(string token, string tokenHash)
        {
            return BCrypt.Net.BCrypt.Verify(token, tokenHash);
        }
    }
}
