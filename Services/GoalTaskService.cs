using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using LeaderDevelop.Model; // Adjust the namespace to match your entity's namespace
using Microsoft.EntityFrameworkCore;
using LeaderDevelop.Data;  // Replace with your DbContext's namespace

public class GoalTaskService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor injection for DbContext and IHttpContextAccessor
    public GoalTaskService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // Method to create a new GoalTask
    public async Task CreateNewGoalTaskAsync(GoalTask newGoalTask)
    {
        // Get the logged-in user's identity
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        // Set the CreateBy and CreateDate fields
        newGoalTask.CreateBy = currentUser;
        newGoalTask.CreateDate = DateTime.Now;

        // Add the new GoalTask to the DbContext and save changes asynchronously
        _context.GoalTasks.Add(newGoalTask);
        await _context.SaveChangesAsync(); // Use async version of SaveChanges
    }

    // Method to update an existing GoalTask
    public async Task UpdateGoalTaskAsync(GoalTask goalTask)
    {
        _context.GoalTasks.Update(goalTask);
        await _context.SaveChangesAsync();
    }

    // Method to retrieve a GoalTask by its ID
    public async Task<GoalTask> GetGoalTaskByIdAsync(int id)
    {
        return await _context.GoalTasks.FirstOrDefaultAsync(gt => gt.Id == id);
    }

    // Method to delete a GoalTask
    public async Task DeleteGoalTaskAsync(int id)
    {
        var goalTask = await _context.GoalTasks.FindAsync(id);
        if (goalTask != null)
        {
            _context.GoalTasks.Remove(goalTask);
            await _context.SaveChangesAsync();
        }
    }
}
