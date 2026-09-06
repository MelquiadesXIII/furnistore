using System.Security.Cryptography;

namespace API.Furnistore.Shared.Common
{
    public static class RandomGenerator
    {
        private const string Alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        public static string GenerateRandomString(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            var chars = new char[size];

            for (var i = 0; i < size; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            return new string(chars);
        }
    }
}
