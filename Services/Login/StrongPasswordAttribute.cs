using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HRM.Services.Login
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            var password = value as string;

            if (string.IsNullOrEmpty(password))
                return false;

            // ความยาวอย่างน้อย 8 ตัวอักษร
            if (password.Length < 8) return false;

            // มีตัวพิมพ์ใหญ่
            if (!Regex.IsMatch(password, "[A-Z]")) return false;

            // มีตัวพิมพ์เล็ก
            if (!Regex.IsMatch(password, "[a-z]")) return false;

            // มีตัวเลข
            if (!Regex.IsMatch(password, "[0-9]")) return false;

            // มีอักขระพิเศษ
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) return false;

            return true;
        }
    }
}
