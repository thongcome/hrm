using Advance.SecurityCore.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Advance.SecurityCore.Data;

// The SecurityCore equivalent of HRM's ApplicationDbContext + the sc_*
// slice of HRMContext, merged into one context because a standalone
// Payroll/Workflow product has no reason to split Identity's own tables
// from the sc_* permission tables the way HRM historically did (HRM keeps
// them in two DbContexts — ApplicationDbContext for AspNetUsers/OpenIddict,
// HRMContext for everything else — purely because HRMContext predates
// Identity's introduction into this codebase).
//
// IdentityDbContext<ApplicationUser> gives AspNetUsers/AspNetRoles/etc. for
// free (matching Program.cs's `AddIdentityCore<ApplicationUser>()
// .AddEntityFrameworkStores<ApplicationDbContext>()` wiring in HRM); the
// sc_* DbSets below are the extra tables this package owns.
//
// Fluent config below is copied from HRM's Model/HRMContext.cs
// OnModelCreating (search for "modelBuilder.Entity<sc_..." there) — same PK
// names / default-value-sql / FK constraint names, since this must map onto
// the SAME live table shape during a staged migration (no schema change,
// no data migration, just a second codebase temporarily pointed at the same
// tables while the cutover proves itself — see EXTRACTION-PLAN.md's staged
// migration order for why that matters).
public class SecurityDbContext : IdentityDbContext<ApplicationUser>
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options) { }

    public DbSet<sc_user> sc_users { get; set; } = null!;
    public DbSet<sc_role> sc_roles { get; set; } = null!;
    public DbSet<sc_user_role> sc_user_roles { get; set; } = null!;
    public DbSet<sc_menu> sc_menus { get; set; } = null!;
    public DbSet<sc_role_menu> sc_role_menus { get; set; } = null!;
    public DbSet<sc_program_role> sc_program_roles { get; set; } = null!;
    // Added beyond the original requested entity list — see sc_program.cs header.
    public DbSet<sc_program> sc_programs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity's own table config first

        modelBuilder.Entity<sc_menu>(entity =>
        {
            entity.HasKey(e => e.menuid).HasName("PK__sc_menu__3B5F7D5C109083C8");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.icon).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isshow).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menucode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.menuorder).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.programid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.small_icon).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.uppermenucode).HasDefaultValueSql("(NULL)");

            // No FK to sc_menugroup here — that table isn't part of this
            // extraction (see sc_menu.cs header). menugroupid stays an
            // unconstrained scalar column, matching the live table's shape
            // (the constraint lives on the sc_menugroup side in HRM, so
            // dropping the EF-level navigation does not itself require a
            // migration against the shared database).
        });

        modelBuilder.Entity<sc_role>(entity =>
        {
            entity.HasKey(e => e.roleid).HasName("PK__sc_role__CD994BF2188A25C3");

            entity.Property(e => e.abbr).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.name).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.rolecode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.upperrole).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_role_menu>(entity =>
        {
            entity.HasKey(e => e.rolemenuid).HasName("PK__sc_rolem__6171F3F863B63A9E");

            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.menu).WithMany(p => p.sc_role_menus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_ROLE_MENU_SC_Menu");

            entity.HasOne(d => d.role).WithMany(p => p.sc_role_menus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_ROLE_MENU_SC_Role");
        });

        modelBuilder.Entity<sc_user>(entity =>
        {
            entity.HasKey(e => e.userid).HasName("PK_sc_user_userid");
            entity.HasIndex(e => new { e.userid, e.password, e.isEmployee }, "IX_sc_user");

            entity.Property(e => e.email).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.empid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.firstname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.invalidpwcount).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.langcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lastinvalidpwd).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lastname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.lasttimelogin).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.loginname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.mobilephone).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.orgcode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.orgid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.orgname).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.password).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.phone).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.pwdexpdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.remindpwd).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.sex_sexid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.social).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.title).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.title_titleid).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.upperuserid).HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<sc_user_role>(entity =>
        {
            entity.HasKey(e => e.user_roleID).HasName("PK__sc_userr__BE814DEE1C50ACD6");

            entity.Property(e => e.enddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.startdate).HasDefaultValueSql("(NULL)");

            entity.HasOne(d => d.role).WithMany(p => p.sc_user_roles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_USER_ROLE_SC_Role");

            entity.HasOne(d => d.user).WithMany(p => p.sc_user_roles)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SC_USER_ROLE_SC_USER");
        });

        modelBuilder.Entity<sc_program>(entity =>
        {
            entity.HasKey(e => e.progid).HasName("PK_SC_PROGRAM");

            entity.Property(e => e.isactive).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.modby).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.moddate).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.progmastercode).HasDefaultValueSql("(NULL)");
        });

        // sc_program_role and AuditLog have no special fluent config in HRM
        // beyond their data annotations — EF picks those up by convention.
    }
}
