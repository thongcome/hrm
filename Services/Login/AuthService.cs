using HRM.Models;
using HRM;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using HRM.Model;

public class AuthService
{
    private readonly HRMContext _context;

    public AuthService(HRMContext context)
    {
        _context = context;
    }

    //public async Task<sc_user?> AuthenticateAsync2(string loginname, string password)
    //{
    //    string hash = HashPassword(password);

    //    var user = await _context.sc_users
    //        .Include(u => u.sc_user_roles)
    //            .ThenInclude(ur => ur.role)
    //        .FirstOrDefaultAsync(u => u.loginname == loginname && u.password == hash && u.isActivate && !u.iscancel);

    //    return user;
    //}
    public async Task<sc_user?> AuthenticateAsync(string loginname, string password)
    {
        string hash = HashPassword(password);
        return await _context.sc_users
            .Include(u => u.sc_user_roles)
                .ThenInclude(ur => ur.role)
                    .ThenInclude(r => r.sc_role_menus)
                        .ThenInclude(rm => rm.menu)
            .FirstOrDefaultAsync(u => u.loginname == loginname && u.password == hash && u.isActivate && !u.iscancel);
    }

    public string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

 