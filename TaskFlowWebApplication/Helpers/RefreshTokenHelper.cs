using System.Security.Cryptography;
using System.Text;

namespace TaskFlowWebApplication.Helpers
{
    public class RefreshTokenHelper
    {
        public static string HashToken(string token)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
