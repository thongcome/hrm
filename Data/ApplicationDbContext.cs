using LeaderDevelop.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaderDevelop.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

   
        public DbSet<TaskMaster> TaskMasters { get; set; }
        public DbSet<TodoTask> TodoTasks { get; set; }
        public DbSet<SubTask> SubTasks { get; set; }

        public DbSet<Activity> Activity { get; set; }
        public DbSet<GoalTask> GoalTasks { get; set; }

        public DbSet<WOL> WOLs { get; set; }

        public DbSet<ScUser> ScUsers { get; set; }  

 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuring one-to-many relationship between TodoTask and SubTask
            modelBuilder.Entity<TodoTask>()
                .HasMany(t => t.SubTasks)
                .WithOne(s => s.TodoTask)
                .HasForeignKey(s => s.TaskId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
