using System.Security.Cryptography;
using System.Text;

namespace API.Furnistore.Application.Auth
{
    public static class RefreshTokenHasher
    {
        public static string Hash(string token) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
