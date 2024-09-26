using LeaderDevelop.Data;
using LeaderDevelop.Model;
using Microsoft.EntityFrameworkCore;


public class UserService
{


    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor injection for DbContext and IHttpContextAccessor
    public  UserService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    public  Task<string> GetUserNameAsync()
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        //var currentUserID = _httpContextAccessor.HttpContext?.User?.Identity.Name;


        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }
        return Task.FromResult(currentUser);
    }

    public String GetUserName()
    {
        // Get the logged-in user's identity
        var currentUser =  _httpContextAccessor.HttpContext?.User?.Identity?.Name;
         //var currentUserID = _httpContextAccessor.HttpContext?.User?.Identity.Name;


        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }
        return currentUser;
    }
}