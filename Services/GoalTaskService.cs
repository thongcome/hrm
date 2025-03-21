using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using HRM.Model; // Adjust the namespace to match your entity's namespace
using Microsoft.EntityFrameworkCore;
using HRM.Data;  // Replace with your DbContext's namespace

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
        //var currentUserID = _httpContextAccessor.HttpContext?.User?.Identity?.Name;


        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        // Set the CreateBy and CreateDate fields
        newGoalTask.CreateBy = currentUser;
        newGoalTask.CreateDate = DateTime.UtcNow;
        newGoalTask.Modby = currentUser;
        newGoalTask.ModDate = DateTime.UtcNow;

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

    ///////// for update progress 
    ///
    public async Task UpdateActivityProgressAsync(int activityId, decimal newProgress)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        //var currentUserID = _httpContextAccessor.HttpContext?.User?.Identity?.Name;


        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }
        var activity = await _context.Activity
                                     .Include(a => a.GoalTask) // Ensure we load the related GoalTask
                                     .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null)
            throw new Exception("Activity not found.");

        // Update the progress of the activity
         
        activity.progress = newProgress;
        activity.ModDate = DateTime.UtcNow;
        activity.Modby = currentUser;
        

        // Save the activity changes
        _context.Activity.Update(activity);
        await _context.SaveChangesAsync();

        // Recalculate and update the GoalTask's progress
        await UpdateGoalTaskProgressAsync(activity.GoalTask.Id);
    }

    // Method to update GoalTask progress by averaging the progress of all related activities
    public async Task UpdateGoalTaskProgressAsync(int goalTaskId)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        //var currentUserID = _httpContextAccessor.HttpContext?.User?.Identity?.Name;


        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }
        var goalTask = await _context.GoalTasks
                                     .Include(gt => gt.Activity) // Include the related activities
                                     .FirstOrDefaultAsync(gt => gt.Id == goalTaskId);

        if (goalTask == null)
            throw new Exception("GoalTask not found.");
        goalTask.Modby = currentUser;
        goalTask.ModDate = DateTime.UtcNow;

        if (goalTask.Activity == null || !goalTask.Activity.Any())
            goalTask.progress = 0; // If no activities, progress is 0
        else
            goalTask.progress = goalTask.Activity.Average(a => a.progress); // Average progress of activities

        // Save the updated GoalTask progress
        _context.GoalTasks.Update(goalTask);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ScUser>> GetAllCoachesAsync()
    {
        // Assuming you have a User entity with a property identifying them as a coach
        return await _context.ScUsers.Where(u => u.IsCoach).ToListAsync();
    }
}



