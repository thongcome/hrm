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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;
using HRM.Interface;
using System.Text;
using HRM.Services.Payroll;
using HRM.Services.Pay;
using HRM.Services.Pay.Calculators;
using System.Security.Claims;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;



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
builder.Services.AddScoped<SystemLanguageSettingsService>();
// Single AuthenticationBuilder reference, reused below — a second,
// separate AddAuthentication(...) call is exactly what caused the
// scheme-conflict bug fixed earlier in this project (the last
// Configure<AuthenticationOptions> delegate silently wins DefaultScheme).
// .AddIdentityCookies() returns a narrower IdentityCookiesBuilder that
// can't chain .AddJwtBearer(...) directly, so both calls go through this
// same authenticationBuilder variable instead of one fluent chain.
var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});
authenticationBuilder.AddIdentityCookies();
// "ExternalApi" resource-server scheme for the ecosystem-wide central AI
// chatbot (and any future HumanOk module) — deliberately NOT the default
// scheme, so it cannot interfere with the cookie-based HR staff login
// above at all; it only registers an additional named scheme and never
// touches DefaultScheme.
authenticationBuilder.AddJwtBearer("ExternalApi", jwtOptions =>
    {
        var externalApiAuth = builder.Configuration.GetSection("ExternalApiAuth");
        var authority = externalApiAuth["Authority"];
        jwtOptions.RequireHttpsMetadata = externalApiAuth.GetValue("RequireHttpsMetadata", true);
        var audience = externalApiAuth["Audience"] ?? "humanok-hrm";

        if (!string.IsNullOrWhiteSpace(authority))
        {
            // Real path: once the central OAuth2/OIDC auth server exists,
            // set ExternalApiAuth:Authority to its issuer URL — signing
            // keys are then discovered automatically via
            // {authority}/.well-known/openid-configuration. No code
            // change needed here when that day comes.
            jwtOptions.Authority = authority;
            jwtOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = audience,
            };
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Dev-only fallback so this resource-server wiring is
            // testable before the central auth server exists. Gated on
            // IsDevelopment() so it can never activate outside local dev
            // even if ExternalApiAuth:DevSigningKey is set by mistake
            // elsewhere — Authority must be configured for this scheme to
            // accept anything outside Development.
            var devKey = builder.Configuration["ExternalApiAuth:DevSigningKey"];
            if (!string.IsNullOrWhiteSpace(devKey))
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "humanok-dev-issuer",
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devKey)),
                };
            }
        }
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// OpenIddict's EF Core store resolves its DbContext directly via DI
// (services.GetRequiredService<ApplicationDbContext>()), not through
// IDbContextFactory<T> — every other context in this app uses the factory
// pattern exclusively, so this exists solely for OpenIddict's
// AddCore().UseDbContext<ApplicationDbContext>() below. Deliberately NOT a
// second plain AddDbContext(...) call — that duplicates EF's own
// IDbContextOptionsConfiguration<T> registration alongside the factory's,
// which trips ASP.NET Core's build-time service-provider validation
// ("Cannot resolve scoped service ... from root provider") even though
// nothing is actually misconfigured. Delegating to the factory that's
// already registered avoids the duplicate options pipeline entirely — DI
// still disposes the created context at end of scope like any other scoped
// service, same as if AddDbContext had registered it directly.
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

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

// Shortens the default DataProtectorTokenProvider lifespan (1 day) for the
// self-service password-reset link (Endpoints/ForgotPasswordEndpoints.cs).
// Safe to change globally: nothing else in this app currently generates a
// token through the "Default" provider (email confirmation is never
// triggered dynamically — every account is created with EmailConfirmed=true
// already set).
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

// Identity.Application's own cookie — this is now the app's ONLY sign-in
// cookie (see removed second AddAuthentication(...) block further down,
// which used to make "Cookies" the real default scheme and cause every
// Blazor page's pre-Identity custom auth-state provider to see
// Identity-signed-in users as anonymous). LoginPath/LogoutPath point at our
// own pages, not Identity's scaffolded /Account/Login.
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

// HRM as OIDC Identity Provider (SSO) for downstream HumanOk apps — see
// Endpoints/OidcEndpoints.cs for the actual authorize/token/userinfo/logout
// handlers, HRM-SSO-Handoff.md for the contract ERP (the first consumer)
// was built against, and Sso_ClientRoleMapping for the per-client role
// story. This is authorization-code + PKCE only (no implicit/client-creds
// flows) — the doc's whole reason for choosing this over a bespoke token
// scheme was to get a standard, client-library-compatible IdP for free.
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetUserInfoEndpointUris("/connect/userinfo")
            .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();

        options.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email);

        // Dev-only ephemeral certs so this works out of the box locally —
        // production needs real, persisted signing/encryption certificates
        // (same "dev fallback, real config required outside Development"
        // shape as ExternalApiAuth:DevSigningKey elsewhere in this file).
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }

        var aspNetCoreBuilder = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();

        // ERP's OIDC client defaults to RequireHttpsMetadata=true (see
        // handoff doc section 5.7) and this app runs plain HTTP locally, so
        // transport security is relaxed only in Development — the real
        // deployment target terminates TLS in front of this app anyway.
        if (builder.Environment.IsDevelopment())
            aspNetCoreBuilder.DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        // Validates access tokens presented to /connect/userinfo — HRM
        // issued them itself, so it can validate them locally rather than
        // calling out to an introspection endpoint.
        options.UseLocalServer();
        options.UseAspNetCore();
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
builder.Services.AddScoped<HRM.Services.Login.UserProvisioningService>();

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

// "ExternalApiCaller" policy for the ecosystem/chatbot resource-server
// surface (Endpoints/ExternalApiEndpoints.cs). This is a plain named
// policy, not a "Menu:"-prefixed one, so MenuPolicyProvider.GetPolicyAsync
// falls through to its wrapped DefaultAuthorizationPolicyProvider and
// finds it here — registering it via a second AddAuthorizationCore(...)
// call is safe because AuthorizationOptions.AddPolicy mutates a shared
// dictionary (additive), unlike AddAuthentication's scalar DefaultScheme
// (see the scheme-conflict comment above).
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("ExternalApiCaller", policy =>
    {
        policy.AuthenticationSchemes.Add("ExternalApi");
        policy.Requirements.Add(new HRM.Services.Login.ExternalApiCallerRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, HRM.Services.Login.ExternalApiCallerHandler>();

// Add Company services to the container.
// Program.cs
builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));
builder.Services.AddScoped(typeof(ISearchableService<>), typeof(BaseService<>));

builder.Services.AddScoped<ICompanyContext, CompanyContext>();
builder.Services.AddHttpContextAccessor();


builder.Services.AddMudServices();

// Add localization service
// Scoped, not Singleton: this is now the live-refresh event bus every
// localized page subscribes to (see LocalizedComponentBase). A Singleton
// here would mean one user toggling language fires StateHasChanged on
// every OTHER connected user's circuit too — it was only safe as a
// Singleton while the sole subscriber was the isolated Testlanguage.razor
// demo page.
builder.Services.AddScoped<LanguageState>();
// Scoped, not Singleton: JsonLocalizationService reads the per-request
// language cookie in its constructor (see the file itself), so a Singleton
// registration would freeze CurrentLanguage at whichever user's cookie
// happened to trigger the first resolution and then serve that same
// language to every user on the server forever. Nothing currently injects
// this interface (every component injects the concrete class, which was
// already correctly Scoped), but fixing it now avoids that landmine for
// whoever reaches for it next.
builder.Services.AddScoped<IJsonLocalizationService, JsonLocalizationService>();

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
builder.Services.AddScoped<HRM.Services.Pay.PayrollAnomalyDetectionService>();
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
builder.Services.AddScoped<HRM.Services.Leave.LeaveBalanceService>();

// ----- Att_* module (Time Tracking & Attendance) -----
builder.Services.AddScoped<HRM.Services.Att.AttendanceAggregationService>();
builder.Services.AddScoped<HRM.Services.Att.AbsenteeismReportService>();
builder.Services.AddScoped<HRM.Services.Att.GpsCheckinService>();
builder.Services.AddScoped<HRM.Services.Att.TimesheetService>();
// ----- end Att_* module -----

// ----- Pos_* module (Position / Headcount Budget) -----
builder.Services.AddScoped<HRM.Services.Pos.HeadcountBudgetService>();
// ----- end Pos_* module -----

// ----- Perf_* module (Performance / KPI) -----
builder.Services.AddScoped<HRM.Services.Perf.PerfAssignmentResolverService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfScoringService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfApprovalService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfMeritService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfGoalService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfConfigCarryForwardService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfCalibrationService>();
builder.Services.AddScoped<HRM.Services.Perf.PerfImprovementPlanService>();
// ----- end Perf_* module -----

// ----- Okr_* module (OKR v2) -----
builder.Services.AddScoped<HRM.Services.Okr.OkrGoalService>();
builder.Services.AddScoped<HRM.Services.Okr.OkrDashboardService>();
// ----- end Okr_* module -----

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
    // Forgot-password requests are an email-spam/enumeration risk more than
    // a brute-force risk (the endpoint itself never reveals whether an
    // account/email matched), so this is capped tighter and over a longer
    // window than "login" rather than sharing that policy.
    options.AddSlidingWindowLimiter("forgot-password", limiterOptions =>
    {
        limiterOptions.PermitLimit = 3;
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
app.MapForgotPasswordEndpoints();
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
app.MapCompanyFileEndpoints();
app.MapEmployeeProfileFileEndpoints();

// Resource-server surface for the ecosystem-wide central AI chatbot — see
// Endpoints/ExternalApiEndpoints.cs. The dev token-minting route is only
// ever mapped in Development, matching the app.UseMigrationsEndPoint()
// pattern above.
app.MapExternalApiEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapExternalApiDevEndpoints();
}

app.MapOidcEndpoints();

// Idempotent client registration for the ERP OIDC client — see
// HRM-SSO-Handoff.md section 4. Runs on every startup but only actually
// creates anything the first time; safe to leave in place permanently
// (matches UserProvisioningService's "ensure exists" idiom elsewhere).
using (var oidcSeedScope = app.Services.CreateScope())
{
    var appManager = oidcSeedScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    if (await appManager.FindByClientIdAsync("erp-web") is null)
    {
        var redirectUri = app.Configuration["Sso:ErpClient:RedirectUri"];
        var postLogoutRedirectUri = app.Configuration["Sso:ErpClient:PostLogoutRedirectUri"];

        if (string.IsNullOrWhiteSpace(redirectUri) || string.IsNullOrWhiteSpace(postLogoutRedirectUri))
        {
            Log.Warning("Sso:ErpClient:RedirectUri/PostLogoutRedirectUri are not configured in appsettings.json — skipping erp-web OIDC client registration.");
        }
        else
        {
            var clientSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "erp-web",
                ClientSecret = clientSecret,
                DisplayName = "ERP (Advance Digital)",
                RedirectUris = { new Uri(redirectUri) },
                PostLogoutRedirectUris = { new Uri(postLogoutRedirectUri) },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Prefixes.Scope + Scopes.OpenId,
                    Permissions.Prefixes.Scope + Scopes.Profile,
                    Permissions.Prefixes.Scope + Scopes.Email,
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            });

            // Shown exactly once, right now — OpenIddict stores only a
            // hash of the secret, so there is no way to retrieve it again
            // later. Copy it and hand it to the ERP team securely per the
            // handoff doc's section 4/6 ("<สุ่ม แล้วส่งให้ทีม ERP อย่างปลอดภัย>").
            Log.Warning("OIDC client 'erp-web' created. ClientSecret (shown once, never logged again): {ClientSecret}", clientSecret);
        }
    }
}

app.Run();
