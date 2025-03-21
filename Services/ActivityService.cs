using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using HRM.Model;  // Adjust the namespace to match your entity's namespace
using Microsoft.EntityFrameworkCore;
using HRM.Data;
using static HRM.Model.Activity;

public class ActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor injection for DbContext and IHttpContextAccessor
    public ActivityService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // Method to create a new Activity and update GoalTask progress
    public async Task CreateNewActivityAsync(Activity newActivity)
    {
        // Get the logged-in user's identity
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        if (currentUser == null)
        {
            throw new InvalidOperationException("Unable to determine the current user.");
        }

        // Set the CreateBy and CreateDate fields
        newActivity.CreateBy = currentUser;
        newActivity.CreateDate = DateTime.Now;
        //newActivity.StatusCode = ActivityStatus.Active;

        var maxOrder = await _context.Activity
           .Where(a => a.TaskId == newActivity.TaskId)
           .MaxAsync(a => (int?)a.Orders) ?? 0;  // If no activities, start at 0

        // Set the new order value
        newActivity.Orders = maxOrder + 1;

        // Add the new activity to the DbContext
        _context.Activity.Add(newActivity);
        await _context.SaveChangesAsync();  // Save changes

        // Recalculate the GoalTask progress after the activity is added
        await RecalculateGoalTaskProgressAsync(newActivity.TaskId);
    }

    // Method to update an Activity and recalculate GoalTask progress
    public async Task UpdateActivityAsync(Activity updatedActivity)
    {
        // Ensure the activity exists in the context
        var existingActivity = await _context.Activity.FindAsync(updatedActivity.Id);
        if (existingActivity == null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        // Update the activity fields
        existingActivity.Name = updatedActivity.Name;
        existingActivity.progress = updatedActivity.progress;
        existingActivity.StatusCode = updatedActivity.StatusCode;
        existingActivity.ModDate = DateTime.Now;
        existingActivity.Modby = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        // Save changes
        await _context.SaveChangesAsync();

        // Recalculate the GoalTask progress after the activity is updated
        await RecalculateGoalTaskProgressAsync(existingActivity.TaskId);
    }

    // Method to delete an Activity and update GoalTask progress
    public async Task DeleteActivityAsync(int activityId)
    {
        var activity = await _context.Activity.FindAsync(activityId);
        if (activity == null)
        {
            throw new InvalidOperationException("Activity not found.");
        }

        // Store GoalTaskId before removing the activity
        int goalTaskId = activity.TaskId;

        // Remove the activity from the context
        _context.Activity.Remove(activity);
        await _context.SaveChangesAsync();

        // Recalculate the GoalTask progress after the activity is deleted
        await RecalculateGoalTaskProgressAsync(goalTaskId);
    }

    // Method to recalculate the progress of a GoalTask based on its activities
    private async Task RecalculateGoalTaskProgressAsync(int goalTaskId)
    {
        // Get the GoalTask and its associated activities
        var goalTask = await _context.GoalTasks
            .Include(g => g.Activity )
            .FirstOrDefaultAsync(g => g.Id == goalTaskId);
        if (goalTask is not null)
        {
            var activeActivities = goalTask.Activity
           .Where(a => a.StatusCode == ActivityStatus.Active);

            if (goalTask.Activity == null || !goalTask.Activity.Any())
            {
                goalTask.progress = 0;  // No activities, set progress to 0
            }
            else
            {
                // Calculate the average progress from all activities
                  goalTask.progress = goalTask.Activity.Average(a => a.progress); // old version

              // await updateProgressToGoal(goalTask.Id);
            }

            // Save the updated progress to the database
            await _context.SaveChangesAsync();
        }
    }

    public async Task updateProgressToGoal(int taskid)
    {
        try
        {
            decimal sum = 0.00M;
            decimal avg = 0.00M;
            
            List<Activity> activitires = new List<Activity>();
             var goal = await _context.GoalTasks.FindAsync(taskid);
            if (goal is not null)
            {


                activitires = await _context.Activity.Where(b => b.TaskId == taskid && b.StatusCode == Activity.ActivityStatus.Active).ToListAsync();

                foreach (var item in activitires)
                {
                    if (item.progress is null)
                    {
                        item.progress = 0;
                    }
                    sum += (decimal)item.progress;

                }
                avg = sum / activitires.Count();

                goal.progress = avg;
                _context.SaveChanges();
            }

        }
        catch (Exception e)
        {
            e.Message.ToString();
        }






    }
}
