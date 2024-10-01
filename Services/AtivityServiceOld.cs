using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using LeaderDevelop.Model;  // Adjust the namespace to match your entity's namespace
using Microsoft.EntityFrameworkCore;
using LeaderDevelop.Data;

public class ActivityServiceOld
{
    private readonly ApplicationDbContext _context;  // Replace 'YourDbContext' with the actual name of your DbContext class
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor injection for DbContext and IHttpContextAccessor
    public ActivityServiceOld(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // Method to create a new Activity
    public async Task CreateNewActivityAsync(Activity newActivity)
    {
        // Get the logged-in user's identity (this could be the username or ID based on your setup)
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        // Set the CreateBy and CreateDate fields
        newActivity.CreateBy = currentUser;
        newActivity.CreateDate = DateTime.Now;

        // Add the new activity to the DbContext and save changes asynchronously
       
        _context.Activity.Add(newActivity);
        await _context.SaveChangesAsync();  // Use async version of SaveChanges
    }


}
