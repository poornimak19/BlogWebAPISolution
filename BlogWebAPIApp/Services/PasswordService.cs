using BlogWebAPIApp.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BlogWebAPIApp.Services
{
    public class PasswordService : IPasswordService
    {
        public byte[] HashPassword(string password, byte[]? existingHashKey, out byte[] hashKey)
        {
            var input = Encoding.UTF8.GetBytes(password);

            if (existingHashKey == null || existingHashKey.Length == 0)
            {
                using var hmac = new HMACSHA512();
                hashKey = hmac.Key;
                return hmac.ComputeHash(input);
            }
            else
            {
                using var hmac = new HMACSHA512(existingHashKey);
                hashKey = existingHashKey;
                return hmac.ComputeHash(input);
            }
        }
    }

}
