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
    .MinimumLevel.Error()  // ��˹��дѺ��úѹ�֡��鹵���� Error
    .WriteTo.Console()  // �ѹ�֡ log ��ѧ Console
    .WriteTo.File("logs/error-log.txt", rollingInterval: RollingInterval.Day)  // �ѹ�֡ log ੾�� Error ŧ���
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
// ----- end Pay_* module -----

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        // OWASP A02/A07 hardening: HttpOnly blocks JS/XSS from reading the
        // cookie, SameSite=Strict blocks it being sent on cross-site
        // requests (CSRF), Secure requires HTTPS (already enforced by
        // UseHttpsRedirection below) — .NET's cookie-auth defaults already
        // set HttpOnly=true, but the rest are worth being explicit about.
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

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
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many login attempts — please wait a moment and try again.", ct);
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

// Plain (non-Blazor-circuit) sign-in endpoint. Login.razor used to call
// HttpContext.SignInAsync directly from an interactive Blazor Server
// component's event handler, which throws "Headers are read-only, response
// has already started" because by the time that handler runs the initial
// HTTP response (and its headers) has already been sent over the open
// SignalR connection — the Set-Cookie header can never be written that way.
// Routing the actual sign-in through a real HTTP endpoint (a fresh
// request/response each time) is the standard fix for cookie auth + Blazor
// Server.
// OWASP A07 lockout thresholds — sc_user.invalidpwcount/lastinvalidpwd
// already existed as columns but nothing ever wrote to them before this;
// unlimited password guessing was possible against any known loginname.
const int MaxFailedAttempts = 5;
var lockoutWindow = TimeSpan.FromMinutes(15);

app.MapPost("/login-handler", async (
    HttpContext httpContext,
    IDbContextFactory<HRMContext> dbFactory,
    IPasswordHasher<sc_user> passwordHasher) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    await using var context = await dbFactory.CreateDbContextAsync();
    var user = await context.sc_users
        .Include(u => u.sc_user_roles).ThenInclude(ur => ur.role).ThenInclude(r => r.sc_role_menus).ThenInclude(rm => rm.menu)
        .FirstOrDefaultAsync(u => u.loginname == username);

    if (user is not null && !user.isdisable && !user.iscancel && user.isActivate)
    {
        var isLockedOut = user.invalidpwcount >= MaxFailedAttempts
            && user.lastinvalidpwd is not null
            && DateTime.Now - user.lastinvalidpwd.Value < lockoutWindow;

        if (!isLockedOut)
        {
            var result = passwordHasher.VerifyHashedPassword(user, user.password ?? "", password);
            if (result != PasswordVerificationResult.Failed)
            {
                user.invalidpwcount = 0;
                user.lasttimelogin = DateTime.Now;
                await context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.firstname + " " + user.lastname),
                    new(ClaimTypes.NameIdentifier, user.userid.ToString()),
                    new("userid", user.userid.ToString()),
                    new("username", user.loginname),
                    new("company_id", user.company_id.ToString()),
                };

                // Role/menu claims — CustomAuthStateProvider.SignInAsync built
                // this exact same shape but is never actually called from
                // anywhere; this endpoint replaced its call site during an
                // earlier fix without carrying the logic over, so every
                // /login-handler session has been missing Role/menu claims
                // ever since. Found while wiring up the new Menu-based
                // [Authorize(Policy="Menu:...")] pages, whose FallbackPolicy
                // would otherwise lock out every real HR login.
                foreach (var ur in user.sc_user_roles.Where(r => r.isactive))
                {
                    if (!string.IsNullOrWhiteSpace(ur.role?.name))
                        claims.Add(new Claim(ClaimTypes.Role, ur.role!.name));
                }
                var menus = user.sc_user_roles
                    .Where(ur => ur.isactive)
                    .SelectMany(ur => ur.role?.sc_role_menus ?? new List<sc_role_menu>())
                    .Where(rm => rm.isactive && rm.menu != null && rm.menu.isactive)
                    .Select(rm => rm.menu!)
                    .Distinct();
                foreach (var menu in menus)
                {
                    if (!string.IsNullOrWhiteSpace(menu.menucode))
                        claims.Add(new Claim("menu", menu.menucode!));
                }

                // Sign in under CookieAuthenticationDefaults.AuthenticationScheme
                // ("Cookies") — confirmed via a throwaway /whoami diagnostic to be
                // the scheme HttpContext.User/Blazor's plain [Authorize] actually
                // resolve against at runtime (the later AddAuthentication(...) call
                // near the bottom of this file's registrations wins over the
                // earlier DefaultScheme = IdentityConstants.ApplicationScheme).
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
            }

            user.invalidpwcount = (user.invalidpwcount ?? 0) + 1;
            user.lastinvalidpwd = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }

    var errorRedirect = "/login?error=1";
    if (!string.IsNullOrEmpty(returnUrl))
        errorRedirect += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    return Results.LocalRedirect(errorRedirect);
}).RequireRateLimiting("login");

// /logout was previously just a dead NavLink — there was no endpoint behind
// it at all, so clicking it did nothing and the session stayed valid until
// its natural 60-minute expiry. Signs out of both schemes since a user may
// be signed in under either "Cookies" (/login-handler) or Identity.Application
// (/Account/Login), depending on which path they used.
app.MapGet("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    return Results.LocalRedirect("/");
});

app.Run();
