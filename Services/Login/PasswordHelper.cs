using System.Security.Cryptography;
using System.Text;

namespace HRM.Services.Login
{


    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            return Guid.NewGuid().ToString("N"); // Random 32-char string
        }

        public static string HashPassword(string password, string salt)
        {
            //using var sha = SHA256.Create();
            //var combined = Encoding.UTF8.GetBytes(password + salt);
            //var hash = sha.ComputeHash(combined);
            //return Convert.ToBase64String(hash);

            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }

}
