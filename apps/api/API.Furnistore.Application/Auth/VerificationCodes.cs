using System.Security.Cryptography;
using System.Text;

namespace API.Furnistore.Application.Auth
{
    internal static class VerificationCodes
    {
        public const int Length = 6;

        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

        public const int MaxAttempts = 5;

        public static string Generate() =>
            RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, Length)).ToString($"D{Length}");

        public static string Hash(string secret, string userId, string code) =>
            Convert.ToHexStringLower(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{userId}:{code}"))
            );

        public static bool Matches(string expectedHash, string actualHash) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedHash),
                Encoding.ASCII.GetBytes(actualHash)
            );

        public static bool IsWellFormed(string? code) =>
            code is { Length: Length } && code.All(char.IsAsciiDigit);
    }
}
