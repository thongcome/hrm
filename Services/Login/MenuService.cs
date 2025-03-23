using HRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
 
    public class MenuService
    {
        private readonly HRMContext _context;

        public MenuService(HRMContext context)
        {
            _context = context;
        }

        public async Task<List<sc_menu>> GetMenusForRolesAsync(List<string> roleNames)
        {
            var menus = await _context.sc_role_menus
                .Include(rm => rm.menu)
                .Include(rm => rm.role)
                .Where(rm => rm.isactive && roleNames.Contains(rm.role.name ?? ""))
                .Select(rm => rm.menu)
                .Distinct()
                .OrderBy(m => m.menuorder)
                .ToListAsync();

            return menus;
        }
    }

 
