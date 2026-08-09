using HRM.Components;
using HRM.Components.Account;
using HRM.Data;
using HRM.Endpoints;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Serilog;
using HRM.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using HRM.Model;
using HRM.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;
using HRM.Interface;
using HRM.Services.Payroll;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using System.Security.Claims;



QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<GoalTaskService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JsonLocalizationService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("SdlAppContext") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddDbContextFactory<HRMContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    // Bridges Identity sign-in to SC_* roles/menus via ApplicationUser.userid
    // -> sc_user.userid — see ScUserClaimsPrincipalFactory for why this
    // doesn't touch ClaimTypes.NameIdentifier.
    .AddClaimsPrincipalFactory<HRM.Services.Login.ScUserClaimsPrincipalFactory>();

// Identity.Application's own cookie — this is now the app's ONLY sign-in
// cookie (see removed second AddAuthentication(...) block further down,
// which used to make "Cookies" the real default scheme and cause every
// Blazor page's CustomAuthStateProvider to see Identity-signed-in users as
// anonymous). LoginPath/LogoutPath point at our own pages, not Identity's
// scaffolded /Account/Login.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    // OWASP A02/A07 hardening (moved here from the removed second
    // AddAuthentication(...).AddCookie(...) block).
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddRazorPages();  // ���������ҹ Razor Pages
                                   // bootstrap blazor



//// Register DbContextFactory for Blazor component and background task usage
//builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

//var supportedCultures = new[] { new CultureInfo("en-US") }; // Change to "th-TH ,en-US " or your preferred culture
//builder.Services.Configure<RequestLocalizationOptions>(options =>
//{
//    options.DefaultRequestCulture = new RequestCulture("en-US"); // Set default culture
//    options.SupportedCultures = supportedCultures;
//    options.SupportedUICultures = supportedCultures;
//});

//builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


//config for email sender
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);
// Load SMTP settings
 var smtpSettings = builder.Configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
// Program.cs
builder.Services.AddTransient<EmailSender>(); // Register the concrete type

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Menu-based authorization: [Authorize(Policy = "Menu:XXX")] on a page is
// resolved dynamically against sc_menu/sc_role_menu via the "menu" claims
// both login paths already attach — see MenuAuthorization.cs.
//
// Deliberately NOT setting a global FallbackPolicy here: this is a large,
// long-lived app with many pre-existing pages (Home, Login, Register, the
// Identity Account flow, /login-handler, /logout, ...) that are
// intentionally public and have no [Authorize] at all. A blanket
// RequireAuthenticatedUser() fallback was tried and immediately broke
// login itself — SignOutAsync -> redirect to "/" -> Home now demanded
// auth -> redirect to /login -> /login now demanded auth too -> infinite
// ReturnUrl-nesting loop. Enumerating every legitimately-public route in
// an app this size to exempt them all safely is its own separate task;
// for now new protected pages must explicitly opt in via
// [Authorize(Policy = "Menu:XXX")] like the ones already converted.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, HRM.Services.Login.MenuPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, HRM.Services.Login.MenuAuthorizationHandler>();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<MenuStateService>();// �红�����ʶҹ�����

// Add Company services to the container.
// Program.cs
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));
builder.Services.AddScoped(typeof(ISearchableService<>), typeof(BaseService<>));

builder.Services.AddScoped<ICompanyContext, CompanyContext>();
builder.Services.AddHttpContextAccessor();


builder.Services.AddMudServices();

// Add localization service
builder.Services.AddSingleton<LanguageState>();
builder.Services.AddSingleton<IJsonLocalizationService, JsonLocalizationService>();

//builder.Services.AddSingleton(smtpSettings);

//builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

// Configure Serilog

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/error-log.txt", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error, rollingInterval: RollingInterval.Day)
    .WriteTo.File("logs/file.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();  // �� Serilog �� logging provider
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
builder.Services.AddScoped<IPasswordHasher<sc_user>, PasswordHasher<sc_user>>();
builder.Services.AddScoped<HRM.Services.Payroll.PayrollCalculationService>();
builder.Services.AddSingleton<PayrollAnalysisService>();

// ----- Pay_* module (new payroll engine, parallel to the legacy Payroll pages) -----
builder.Services.AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>();
builder.Services.AddScoped<OvertimeEarningsCalculator>();
builder.Services.AddScoped<LoanDeductionCalculator>();
builder.Services.AddScoped<HRM.Services.Pay.PayrollCalculationService>();
builder.Services.AddScoped<PayrollWorkflowService>();
builder.Services.AddSingleton<PrivateFileStorage>();
builder.Services.AddScoped<PayslipGenerationService>();
builder.Services.AddScoped<BankFileExportService>();
builder.Services.AddScoped<GLExportService>();
builder.Services.AddScoped<HRM.Services.Pay.SeveranceService>();
builder.Services.AddScoped<HRM.Services.Pay.EmployeeLoanService>();
builder.Services.AddScoped<HRM.Services.Pay.InsuranceAutoEnrollService>();
builder.Services.AddScoped<HRM.Services.Pay.DocumentExpiryService>();
builder.Services.AddScoped<HRM.Services.Audit.IAuditLogger, HRM.Services.Audit.AuditLogger>();
// ----- end Pay_* module -----

builder.Services.AddScoped<HRM.Services.Workflow.WorkflowEngineService>();
builder.Services.AddScoped<HRM.Services.Org.OrgChangeRequestService>();

// ----- Att_* module (Time Tracking & Attendance) -----
builder.Services.AddScoped<HRM.Services.Att.AttendanceAggregationService>();
builder.Services.AddScoped<HRM.Services.Att.AbsenteeismReportService>();
builder.Services.AddScoped<HRM.Services.Att.GpsCheckinService>();
builder.Services.AddScoped<HRM.Services.Att.TimesheetService>();
// ----- end Att_* module -----

// ----- Perf_* module (Performance / KPI) -----
builder.Services.AddScoped<HRM.Services.Perf.PerfAssignmentResolverService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfScoringService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfApprovalService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfMeritService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfGoalService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfConfigCarryForwardService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfCalibrationService>();
// ----- end Perf_* module -----

// ----- info_message module (HR announcements) -----
builder.Services.AddScoped<HRM.Services.Hr.InfoMessageService>();
builder.Services.AddScoped<HRM.Services.Hrd.LifecycleTaskService>();
builder.Services.AddScoped<HRM.Services.Exp.ExpenseClaimService>();
builder.Services.AddScoped<HRM.Services.Hr.DisciplinaryActionService>();
builder.Services.AddScoped<HRM.Services.Hr.GrievanceService>();
builder.Services.AddScoped<HRM.Services.Hr.RewardCaseService>();
// ----- end info_message module -----

// ----- Idp_* module (Individual Development Plan) -----
builder.Services.AddScoped<HRM.Services.Idp.IdpAssessmentService>();
builder.Services.AddScoped<HRM.Services.Idp.IdpPlanService>();
// ----- end Idp_* module -----

// ----- Lms_* module (Learning Management System, HRD Phase 4) -----
builder.Services.AddScoped<HRM.Services.Lms.LmsEnrollmentService>();
builder.Services.AddScoped<HRM.Services.Lms.LmsQuizService>();
builder.Services.AddScoped<HRM.Services.Lms.LmsTrainingBudgetService>();
// ----- end Lms_* module -----

// ----- Km_* module (Knowledge Management, HRD Phase 6) -----
builder.Services.AddScoped<HRM.Services.Km.KmArticleService>();
// ----- end Km_* module -----

// ----- Talent_* module (Talent Management / 9-Box) -----
builder.Services.AddScoped<HRM.Services.Talent.TalentGridService>();
builder.Services.AddScoped<HRM.Services.Succession.SuccessionService>();
builder.Services.AddScoped<HRM.Services.Career.CareerPathService>();
builder.Services.AddScoped<HRM.Services.OrgDev.WorkforcePlanService>();
builder.Services.AddScoped<HRM.Services.OrgDev.LeadershipDevelopmentService>();
builder.Services.AddScoped<HRM.Services.OrgDev.ChangeInitiativeService>();
builder.Services.AddScoped<HRM.Services.OrgDev.CultureAssessmentService>();
// ----- end Talent_* module -----

// ----- Rec_* module (Recruitment / ATS) -----
builder.Services.AddScoped<HRM.Services.Rec.RecRequisitionService>();
builder.Services.AddScoped<HRM.Services.Rec.RecJobPostingService>();
builder.Services.AddScoped<HRM.Services.Rec.RecApplicationService>();
builder.Services.AddScoped<HRM.Services.Rec.RecInterviewService>();
builder.Services.AddScoped<HRM.Services.Rec.RecOfferService>();
// ----- end Rec_* module -----

// ----- Eng_* module (Employee Engagement) -----
builder.Services.AddScoped<HRM.Services.Engagement.QuestionTemplateService>();
builder.Services.AddScoped<HRM.Services.Engagement.SurveyService>();
builder.Services.AddScoped<HRM.Services.Engagement.ActionPlanService>();
builder.Services.AddScoped<HRM.Services.Engagement.RecognitionService>();
// ----- end Eng_* module -----

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// OWASP A07 (brute-force login guessing) — /login-handler below has no
// other throttle, so cap it at the network layer: 5 attempts per IP per
// minute, sliding window, extra requests get a 429 instead of hitting the
// password hasher at all.
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 4;
        limiterOptions.QueueLimit = 0;
    });
    options.AddSlidingWindowLimiter("career-apply", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.SegmentsPerWindow = 4;
        limiterOptions.QueueLimit = 0;
    });
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many attempts — please wait a moment and try again.", ct);
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});
//app.UseRequestLocalization();




app.UseHttpsRedirection();

app.UseStaticFiles();

// Standard Blazor Web App pipeline order: auth middleware must run BEFORE
// UseAntiforgery, since antiforgery token validation for authenticated form
// posts (e.g. Identity's Login/Register pages) depends on the user
// principal already being established. A previous fix attempt here
// (see the removed "remark becuase solved antifogery mismath" comment)
// worked around a symptom of this by disabling auth entirely instead of
// fixing the order — that broke [Authorize] everywhere, so it was
// re-enabled again below the antiforgery call, which is what caused
// "A valid antiforgery token was not provided" on /Account/Login.
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();


//app.UseRouting();

app.MapRazorPages();  // �Դ��ҹ��鹷ҧ Razor Pages �ͧ Identity
app.MapPayrollFileEndpoints();

// Login/logout endpoints — see Endpoints/LoginEndpoints.cs. Routing sign-in
// through a real HTTP endpoint (not a Blazor circuit event handler) is
// required for cookie auth + Blazor Server: Login.razor calling
// HttpContext.SignInAsync directly from a component event handler throws
// "Headers are read-only, response has already started" because the
// SignalR-connection response has already begun by the time that handler
// runs.
app.MapLoginEndpoints();
app.MapLanguageEndpoints();
app.MapEssFileEndpoints();
app.MapExpenseFileEndpoints();
app.MapWorkflowFileEndpoints();
app.MapInfoMessageFileEndpoints();
app.MapHrFileEndpoints();
app.MapCareerEndpoints();
app.MapRecFileEndpoints();
app.MapLmsFileEndpoints();
app.MapKmFileEndpoints();
app.MapOrgChartFileEndpoints();

app.Run();
