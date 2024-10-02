using LeaderDevelop.Data;
using LeaderDevelop.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor injection for DbContext and IHttpContextAccessor
    public UserService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // Method to get the current logged-in user's username
    public string GetUserName()
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        return currentUser;
    }

    // Async method to get the current logged-in user's username
    public Task<string> GetUserNameAsync()
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        return Task.FromResult(currentUser);
    }

    // Method to get the current logged-in user's ID (from ClaimTypes.NameIdentifier)
    public string GetUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            throw new InvalidOperationException("Unable to determine the current user ID.");
        }

        return userId;
    }

    // Async method to get the current logged-in user's ID
    public Task<string> GetUserIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            throw new InvalidOperationException("Unable to determine the current user ID.");
        }

        return Task.FromResult(userId);
    }

    // Method to get the current logged-in user's email
    public string GetUserEmail()
    {
        var email = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        if (email == null)
        {
            throw new InvalidOperationException("Unable to determine the current user's email.");
        }

        return email;
    }

    // Method to get all roles of the current logged-in user
    public List<string> GetUserRoles()
    {
        var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
                          .Select(role => role.Value).ToList();

        if (roles == null || !roles.Any())
        {
            throw new InvalidOperationException("No roles assigned to the current user.");
        }

        return roles;
    }

    // Method to get all claims for the current logged-in user
    public Dictionary<string, string> GetAllUserClaims()
    {
        var claims = _httpContextAccessor.HttpContext?.User?.Claims;

        if (claims == null)
        {
            throw new InvalidOperationException("Unable to retrieve claims for the current user.");
        }

        return claims.ToDictionary(claim => claim.Type, claim => claim.Value);
    }

 
}
