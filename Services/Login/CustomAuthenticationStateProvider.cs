using HRM.Model;
using HRM.Models;
using HRM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly MenuStateService menuService;

    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser = null!;

    public CustomAuthStateProvider(MenuStateService menuService)
    {
        this.menuService = menuService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser ?? _anonymous));
    }

    public Task SignInAsync(sc_user user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.loginname),
            new(ClaimTypes.NameIdentifier, user.userid.ToString())
        };

        // ✅ Add role claims
        foreach (var ur in user.sc_user_roles.Where(r => r.isactive))
        {
            if (!string.IsNullOrWhiteSpace(ur.role?.name))
                claims.Add(new Claim(ClaimTypes.Role, ur.role!.name));
        }

        // ✅ Load menus and store in MenuService
        var menus = user.sc_user_roles
            .SelectMany(ur => ur.role?.sc_role_menus ?? new List<sc_role_menu>())
            .Where(rm => rm.isactive && rm.menu != null && rm.menu.isactive)
            .Select(rm => rm.menu!)
            .Distinct()
            .ToList();

        menuService.AllowedMenus = menus;

        // ✅ Add menu claims (optional for quick access)
        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.menucode))
                claims.Add(new Claim("menu", menu.menucode!));
        }

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "Custom"));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        _currentUser = _anonymous;
        menuService.AllowedMenus = new(); // ✅ clear menu on logout
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        return Task.CompletedTask;
    }
}
