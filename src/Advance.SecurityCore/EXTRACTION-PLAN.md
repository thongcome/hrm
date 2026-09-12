# Advance.SecurityCore — Extraction Plan (Phase 0 prep)

Written 2026-09-12 against `D:\GitWorkspace\HRM` (branch `develop`, commit `e9352bf`), in worktree
`D:\GitWorkspace\HRM\.claude\worktrees\agent-a8c963ccfa2d417b6`. Context: `docs\Plan_Split_Payroll_Workflow_v1.1.md`
section 3.2 — Advance.Payroll and Advance.Workflow are being sold **without** HumanOk, so each needs
its own login/menu/rights/audit "host shell". This document is the real deliverable of this pass; the
`src/Advance.SecurityCore/*` code is a first-cut skeleton to make the seams concrete, **not** a
finished package. Nothing under `Services/`, `Model/`, `Components/`, `Endpoints/`, `Middleware/`,
`Data/`, `Program.cs`, `HRM.csproj`, or `HRM.sln` was modified — everything lives under this new
`src/Advance.SecurityCore/` folder.

---

## 1. Program.cs — every line range SecurityCore will own

Read in full (958 lines). Line numbers below are from `Program.cs` on commit `e9352bf`.

| Lines | What it does | SecurityCore owns it? |
|---|---|---|
| 39-45 | `AddRazorComponents`, `AddCascadingAuthenticationState`, `IdentityUserAccessor`/`IdentityRedirectManager` scoped, `IdentityRevalidatingAuthenticationStateProvider` as the `AuthenticationStateProvider` | **Yes** — `IdentityUserAccessor`/`IdentityRedirectManager`/`IdentityRevalidatingAuthenticationStateProvider` are copied under `Advance.SecurityCore.Web/Components/Account/`. `AddRazorComponents`/`AddCascadingAuthenticationState` stay host calls (every Blazor Server app needs them regardless of SecurityCore). |
| 52-64 | Single `AddAuthentication(...)` call: `DefaultScheme = IdentityConstants.ApplicationScheme`, `DefaultSignInScheme = IdentityConstants.ExternalScheme`, `.AddIdentityCookies()` | **Yes** — this is the actual login scheme. Reproduced in `ServiceCollectionExtensions.AddAdvanceSecurityCore`. |
| 65-113 | `authenticationBuilder.AddJwtBearer("ExternalApi", ...)` — a **named, non-default** resource-server scheme for the ecosystem-wide AI chatbot | **No.** This is a separate concern (HRM acting as an API resource server for another app), not needed for a standalone Payroll/Workflow product's own login. Could become its own small add-on later; not part of this extraction. |
| 115-144 | `foreach (var sso in ssoProviders) authenticationBuilder.AddOpenIdConnect(...)` — AD/OIDC SSO scaffold, inert by default | **Borderline — not done in this pass.** The mechanism (register N named OIDC schemes from config, sign in via `IdentityConstants.ExternalScheme`) is generic enough to belong in SecurityCore eventually, and `ExternalSsoEndpoints`/`ExternalIdentityProvisioningService` (the callback + sc_user resolution) were **not** in this extraction's requested file list. Flagged as good candidate for a **later, deliberate** SecurityCore pass — not blocking. |
| 146-170 | `AddDbContextFactory<ApplicationDbContext>` + the scoped `ApplicationDbContext` shim comment + `AddDbContextFactory<HRMContext>` | **Half.** The `ApplicationDbContext` half (Identity's own tables) is what `SecurityDbContext` replaces — see section 4. The `HRMContext` half (everything else) obviously stays HRM's own. |
| 177-193 | `PasswordPolicy`/`SecurityAlerts` options binding + singletons, **plus** two `EFilingFormats.SsoPrefixCode*` static-field assignments interleaved in the same block | **Mostly yes, one line pair is not.** `PasswordPolicyOptions`/`PasswordPolicyService`/`SecurityAlertOptions`/`SecurityAlertCounter`/`SecurityAlertService` registrations are copied into `AddAdvanceSecurityCore`. The `HRM.Services.Pay.EFilingFormats.SsoPrefixCode*` lines (184-186) are Payroll e-Filing config that happens to sit in the same code block for no structural reason — pure coincidence of file layout, not a SecurityCore concern. |
| 195-221 | `AddIdentityCore<ApplicationUser>(...)` — password/lockout options, `.AddEntityFrameworkStores<ApplicationDbContext>()`, `.AddSignInManager()`, `.AddDefaultTokenProviders()`, `.AddClaimsPrincipalFactory<ScUserClaimsPrincipalFactory>()` | **Yes**, entirely — this is the core of the package. `.AddEntityFrameworkStores<ApplicationDbContext>()` becomes `.AddEntityFrameworkStores<SecurityDbContext>()`. |
| 223-232 | `Configure<DataProtectionTokenProviderOptions>` (1-hour token lifespan for the reset-link provider) | **Yes.** |
| 234-250 | `ConfigureApplicationCookie` — `LoginPath`/`LogoutPath`/`ExpireTimeSpan`/cookie hardening | **Yes.** |
| 252-305 | `AddOpenIddict()` — HRM **as an OIDC Identity Provider** for downstream apps (ERP today), authorization-code+PKCE, `UseDbContext<ApplicationDbContext>()` | **No — deliberate, reasoned exclusion.** This is a materially different concern from "this app's own users can log in": it's HRM acting as an **SSO hub for other applications**. A standalone Payroll/Workflow product needs to authenticate its *own* users; it does not need to *issue tokens to other apps* unless it's specifically being positioned as an identity hub (the `hrm_future_sso_direction` memory note — "a separate OpenIddict/Keycloak auth server eventually for multiple HumanOk modules" — is exactly this, and explicitly a *later*, *separate* effort). Recommendation: this stays HRM-specific (or becomes its own future `Advance.Auth`/IdP package) and is **not** part of SecurityCore. |
| 307 | `AddRazorPages()` | Generic ASP.NET Core plumbing every host needs anyway (Identity's scaffolding uses Razor *Components*, not Razor *Pages*, in this app — this call exists for HRM's own Razor Pages elsewhere). Host's own call. |
| 328-343 | `IEmailSender`/`EmailSender` (SMTP) registration + the removed-legacy-hasher comment | **Interface only.** SecurityCore code depends on `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender` (an ASP.NET Core interface, not HRM's), never the concrete SMTP `EmailSender` class — a host supplies its own implementation. Line 452 (`AddScoped<IPasswordHasher<sc_user>, PasswordHasher<sc_user>>()`) is a **separate, unexplained-in-comments** registration — flagged below as needing verification before moving anywhere. |
| 350-351 | `LdapAuthService` + `ExternalIdentityProvisioningService` | **Half.** `LdapAuthService` copied (generic LDAP bind, reads only from config) — see section 5 "additions". `ExternalIdentityProvisioningService` was not read/copied; likely resolves an incoming SSO identity to an `sc_user`, which may have HRM-specific matching rules — needs its own look before moving. |
| 353-357 | `AddMemoryCache()`, `AddScoped<ProgramRoleService>()` | **Yes** — AD.CRUDManage. |
| 359-382 | `MenuPolicyProvider`/`MenuAuthorizationHandler` as the single `IAuthorizationPolicyProvider`/handler, **and** `ProgramAuthorizationHandler` (superseded POC) registered alongside it, `AddAuthorizationCore()` | **Split.** `MenuPolicyProvider`/`MenuAuthorizationHandler` (the `Menu:XXX` mechanism) copied. `ProgramAuthorizationHandler`/`ProgramRequirement` (`Services/Login/ProgramAuthorization.cs`, the `Program:XXX` claim-based POC) **deliberately not copied** — CLAUDE.md calls it superseded, "don't extend it to new pages." See section 3. |
| 384-400 | `ExternalApiCaller` policy + handler (ecosystem chatbot) | **No** — HRM/ecosystem-specific, not a SecurityCore concern. |
| 404-411 | `IBaseService<>`/`ISearchableService<>`, `ICompanyContext`, `AddHttpContextAccessor`, `AddMudServices` | **No** (generic app scaffolding / MudBlazor UI choice) except `AddHttpContextAccessor` which `AddAdvanceSecurityCore` also calls (idempotent either way — ASP.NET Core's DI allows the duplicate registration). |
| 452 | `AddScoped<IPasswordHasher<sc_user>, PasswordHasher<sc_user>>()` | **Needs verification, not moved yet.** No comment explains a live call site; `sc_user.password` itself looks like a vestige of the pre-Identity auth path (`AuthService`/`PasswordHelper`, explicitly noted as dead a few lines above at 337-342). Before this hasher registration moves anywhere, **grep for actual `IPasswordHasher<sc_user>` injections** — if none exist, it's dead scaffolding like `AuthService` was, not a SecurityCore requirement. |
| 476 | `AddScoped<HRM.Services.Audit.IAuditLogger, HRM.Services.Audit.AuditLogger>()` | **Yes**, copied. |
| 478-482 | `AuditArchiveOptions`/`AuditArchiveService` (archives old `AuditLog` rows to a file share) | **No, not in this pass — but a good later candidate.** The *policy* (never purge under 90 days, พ.ร.บ. คอมพิวเตอร์) is generic; the *mechanism* (file-share archiving) wasn't requested and touches deployment topology a standalone Payroll/Workflow SaaS might handle differently (e.g. a managed blob store instead of a UNC path). Left as HRM-specific for now. |
| 649-688 | `AddAuthorization()`, `AddRateLimiter` with THREE named policies: `"login"`, `"career-apply"`, `"forgot-password"` | **Partial.** Only `"login"` is a SecurityCore concern (protects `/login-handler`, which SecurityCore owns) — copied into `AddAdvanceSecurityCore`. `"career-apply"` (recruitment) and `"forgot-password"` (HRM's own `ForgotPasswordEndpoints`, not copied) stay HRM-specific. |
| 722 | `app.UseMiddleware<HRM.Middleware.SecurityHeadersMiddleware>()` (OWASP A05 CSP + headers) | **No, not in this pass.** Fully generic in principle (no HRM-specific header value found on a skim) — but wasn't in the task's requested file list, and the architecture doc places generic security/audit/header concerns at the lower "Advance.Platform" layer, not SecurityCore specifically (see `docs/Plan_Split_Payroll_Workflow_v1.1.md` section 3, the `ระดับ 0-1 Advance.Platform` row). Recommend it move to whichever package ends up being `Advance.Platform`, not SecurityCore, when that package exists. |
| 751-763 | `UseAuthentication()`, `UseAuthorization()`, `ForcePasswordChangeMiddleware`, `RequireMfaMiddleware`, `UseRateLimiter()`, `UseAntiforgery()` | **Split, order matters.** `ForcePasswordChangeMiddleware`/`RequireMfaMiddleware` are copied and called from `UseAdvanceSecurityCore()`. `UseAuthentication`/`UseAuthorization`/`UseRateLimiter`/`UseAntiforgery` remain **host** pipeline calls that must run in this same relative order — `UseAdvanceSecurityCore()` slots in between `UseAuthorization()` and `UseRateLimiter()`, exactly where HRM has it today. See `ApplicationBuilderExtensions.cs`'s header comment. |
| 769 | `app.MapAdditionalIdentityEndpoints()` | **Yes**, called from `UseAdvanceSecurityCore()`. |
| 778-788 | `MapLoginEndpoints()`, `MapPasswordPolicyEndpoints()`, `MapExternalSsoEndpoints()`, `MapForgotPasswordEndpoints()`, plus several module-specific `Map*FileEndpoints()` calls | **One of four.** Only `MapLoginEndpoints()` copied. `PasswordPolicyEndpoints.cs` (the `/force-change-password-handler` POST), `ExternalSsoEndpoints.cs`, `ForgotPasswordEndpoints.cs` were **not** in the requested file list and are **not** mapped by `UseAdvanceSecurityCore()` — flagged explicitly in that file's header comment so a host doesn't assume force-change/forgot-password forms work end-to-end from this package alone yet. |
| 906-936 | AD.CRUDManage + menu seeding: `ScProgramRouteSeeder.SeedAsync`, `ProgramRoleService.SeedAsync`, `ScMenuNavSeeder.SeedAsync` — **plus**, interleaved in the same startup block, half a dozen HRM-specific seeders (`EmployeeDocTypeSeeder`, `EmployeeTypeRoleSeeder`, `PositionRoleSeeder`, `DerivedRoleSyncService`, `WelfareWorkflowSeeder`, `WorkflowStateChangeSeeder`, `EngRedeemWorkflowSeeder`) | **Three of many.** The three route/menu seeders are copied (as `SeedAdvanceSecurityCoreAsync()`). Everything else in that startup block is a specific module's own bootstrap data and stays in HRM's `Program.cs`. |

**Not examined line-by-line** (obviously out of scope, business-module DI registrations occupying most of lines 402-648): every `Pay_*`/`Att_*`/`Perf_*`/`Rec_*`/etc. service registration. None of it touches auth/menu/rights.

---

## 2. Claims classification (`ScUserClaimsPrincipalFactory.cs`, every `identity.AddClaim(...)`)

| Claim | Classification | Notes |
|---|---|---|
| `sc_userid` | **Generic — SecurityCore** | Copied as-is. |
| `permversion` | **Generic — SecurityCore** | Copied. The *bump* side (`HRMContext.PermVersion.cs`, an EF `SaveChanges` interceptor that increments it when a role/menu/scope changes) was **not** copied — see section 6. Without it the column exists and is read, but nothing currently writes a fresh value from SecurityCore's own `SecurityDbContext`. Flagged as follow-up. |
| `pwd_change_required` (`ForcePasswordChangeMiddleware.ClaimType`) | **Generic — SecurityCore** | Copied. |
| `pwd_expires_in_days` | **Generic — SecurityCore** | Copied. |
| `fullname` | **Generic — SecurityCore** | Copied (`sc_user.firstname`/`lastname` are in-scope columns). |
| `ClaimTypes.Role` (one per active `sc_user_role`) | **Generic — SecurityCore** | Copied. |
| `menu` / `menu_edit` (one per active `sc_role_menu` grant) | **Generic — SecurityCore** | Copied — this is the load-bearing claim pair every `[Authorize(Policy="Menu:XXX")]` page depends on. |
| `sessionid` (`sc_user_session` row stamped every sign-in) | **Generic in concept, NOT implemented in this pass** | `sc_user_session` was not in the extraction's requested entity list. `ScUserClaimsPrincipalFactory.cs` here does **not** stamp a session or emit this claim; `IdentityRevalidatingAuthenticationStateProvider.cs` here does **not** re-check it. Session-revocation (an admin force-logging-out a user) simply does not exist yet in this package. Real gap, not a design choice — see section 6. |
| `program` (one per active `sc_role_program` grant, feeds the superseded `Program:XXX` policy) | **Generic-shaped, deliberately NOT carried** | `sc_role_program` was not in-scope, and CLAUDE.md says the POC it feeds should not be extended. AD.CRUDManage (`ProgramRoleService`, fully copied) is the mechanism that replaces it for new pages. |
| `scope_unrestricted` / `scope_company` / `scope_org` / `scope_costcenter` (from `sc_role_scope`) | **Generic-shaped, NOT implemented in this pass** | `sc_role_scope` (Advance Security slice 1 — data-scope restriction) was not in-scope. A host relying on scope claims today would lose them if it switched its claims factory to this package as-is. Flagged as follow-up (would need `sc_role_scope` added to Domain/Data first). |
| `empno` (`sc_user.empid`) | **HRM-specific — via `IHostClaimsEnricher`** | Only meaningful when the host has an employee master at all. |
| `payroll_company` (via `PayrollCompanyResolver`, joins `com_company`/`Hremployee`) | **HRM-specific — via `IHostClaimsEnricher`** | `PayrollCompanyResolver.cs` itself was read and confirmed to depend on `com_company` and `Hremployee` — neither in SecurityCore's scope. A host's `IHostClaimsEnricher` implementation calls the host's own resolver against the host's own context. |

---

## 3. Menu authorization: what stays generic vs. per-product

- **Stays generic, unconditionally:** the `Menu:XXX` policy mechanism itself (`MenuPolicyProvider`, `MenuRequirement`, `MenuAuthorizationHandler`) — it only ever asks "does this principal have a `menu` claim equal to this string", never caring what the string means. Copied verbatim into `Advance.SecurityCore.Services`.
- **Per-product, by design:** the actual **menucodes** (`PAY_ADMIN`, `SYS_ADMIN`, workflow-specific codes, etc.) and the drawer tree they gate. HRM's `ScMenuNavCatalog.cs` hardcoded ~30 groups and every link as one static list — fine for one product, wrong for three. Replaced here with `IMenuNavContributor` (`Groups`/`Links`, same record shapes as the original catalog) — each module/product registers its own contributor via DI, and `ScMenuNavSeeder.SeedAsync` now takes `IEnumerable<IMenuNavContributor>` and concatenates all of them. Concretely: HRM's existing `ScMenuNavCatalog.cs` becomes the body of one `HrmMenuNavContributor : IMenuNavContributor`; a standalone Payroll product wanting its own `PAY_ADMIN` tree registers `PayrollMenuNavContributor`; Workflow does the same. No single team has to keep one 400-line file in sync across three products' worth of menu items.
- **`Program:XXX` (`ProgramAuthorizationHandler`) does NOT get this treatment** — per CLAUDE.md it's a superseded proof-of-concept on its way out, not a pattern to generalize further. AD.CRUDManage (`ProgramRoleService`) is the mechanism that replaces it, and that one *is* fully generic already (see section 4).

---

## 4. AD.CRUDManage / `ProgramRoleService` — confirmed generic, one adjustment made

Read in full. Every method operates purely on **route-path strings** and **role IDs** — `NormalizeRouteTemplate`, `ScanRoutedPaths`, `ProgPathCovers`, `ResolveRights` contain zero HRM-specific concepts. The only HRM-specific detail anywhere in the file was a hardcoded scan target:

```csharp
var componentAssembly = typeof(HRM.Components.App).Assembly;
```

This is now a parameter (`IEnumerable<Assembly> routeAssemblies`, threaded through from `SecurityCoreOptions.RouteAssemblies`) on both `ProgramRoleService.ScanRoutedPaths`/`SeedAsync` and the sibling `ScProgramRouteSeeder.SeedAsync` (which seeds the legacy `sc_program` route-registry table off the same scan — see section 5 for why `sc_program` is part of this extraction even though it wasn't originally listed). No other change was needed. This confirms the task's suspicion that AD.CRUDManage "is already fully generic" — it was, modulo that one assembly reference.

---

## 5. Additions beyond the originally requested entity/file list

The task named 7 entities (`sc_user`, `sc_role`, `sc_user_role`, `sc_menu`, `sc_role_menu`, `sc_program_role`, `AuditLog`) and asked to copy `ScProgramRouteSeeder`. That seeder writes into `sc_program` (the legacy route-registry table `ProgramRoleService.RequireAsync`/rights checks do **not** use — `sc_program_role` is the rights table; `sc_program` is a separate, older "register every route as a legacy JSP-style program" registry `ScProgramRouteSeeder` populates). Copying the seeder without the table it writes to would be worse than adding it, so `sc_program.cs` was added to `Advance.SecurityCore.Domain` — flagged here rather than silently expanding scope.

Similarly, `LoginEndpoints.cs` (requested) depends on `Services/Auth/LdapAuthService.cs` (not requested) for the AD/LDAP bind branch. Read in full and confirmed fully generic (reads everything from `IConfiguration`, no HRM-specific concept) — copied into `Advance.SecurityCore.Web`.

---

## 6. Domain copy trims (things silently narrower than the HRM original — read before assuming feature parity)

- **`sc_user`**: `company` navigation to `com_company` removed (that table wasn't in scope — `company_id` stays a plain `long` column, same DB shape). Navigations to `job_user_list`, `wf_adhoc_user`, `wf_custom_user`, `emp_checkin` removed (Workflow/Attendance domain, not SecurityCore). **`sc_user_session` is not modeled at all** — see below.
- **`sc_role`**: `company` navigation removed (same reasoning). `sc_role_scope` and `sc_role_program` navigations removed — see the claims table above for what that costs.
- **`sc_menu`**: `menugroup` navigation to `sc_menugroup` removed (single-row legacy lookup table, every existing row uses `menugroupid=1`; kept as a plain scalar column so the live table's FK shape is unaffected).
- **Session revocation (`sc_user_session`) does not exist in this package.** This is the single largest behavioral gap versus HRM today: no session-stamping at sign-in, no revocation check on revalidation, no admin "kick this session out" capability. If a real cutover needs this on day one, `sc_user_session.cs` needs adding to Domain/Data and both `ScUserClaimsPrincipalFactory` and `IdentityRevalidatingAuthenticationStateProvider` need the logic restored — mechanical, but not done here.
- **The `AuditLog` write-side automatic hook does not exist in `SecurityDbContext`.** HRM's `Model/HRMContext.Audit.cs` overrides `SaveChangesAsync`/`SaveChanges` to walk the `ChangeTracker` and log every Create/Update/Delete on *every* entity, automatically, for the whole app. That override lives on `HRMContext` itself (a ~160-line, carefully-commented file the team calls "the highest-risk file in the audit-log feature") and was **not** ported to `SecurityDbContext` — doing so honestly is a bigger, more deliberate task than a Phase-0 skeleton (it needs the same "which entities are excluded" policy, the same PK-resolution-via-EF-metadata trick, and needs deciding whether it audits SecurityCore's own tables only or expects to be layered under a host's context via partial classes / a shared base class). `IAuditLogger`/`AuditLogger` (the **explicit** `LogAccessAsync`/`LogChangeAsync` calls PDPA-sensitive pages make) **are** copied and fully functional. `AuditMasking.cs`, mentioned in the task prompt as possibly added by a background agent, **does not exist anywhere in the HRM repo** as of this extraction (confirmed by search) — nothing to copy.
- **`RequireMfaMiddleware`'s admin gate is hardcoded to the menucode `"SYS_ADMIN"`.** Copied as-is (matches HRM); a later version could make this configurable via `SecurityAlertOptions` instead of a compile-time constant, since a standalone product may not use that exact code.

---

## 7. Things that should NOT move to SecurityCore — ever

- **`ProgramAuthorization.cs` (`Program:XXX` claims-based POC).** CLAUDE.md is explicit that this is superseded by AD.CRUDManage; carrying it into a brand-new shared package would re-legitimize a pattern the team has already decided to retire. If any page still depends on it, migrate that page to AD.CRUDManage instead of moving the POC.
- **`PayrollCompanyResolver.cs`.** Read in full — it joins `com_company` and `Hremployee`, both firmly HRM/payroll concepts. This is precisely the shape `IHostClaimsEnricher` exists to keep out of SecurityCore.
- **`ExternalIdentityProvisioningService`, `AuditArchiveService`, `EmailSender` (SMTP), OpenIddict/IdP issuance, `SecurityHeadersMiddleware`.** Each is either genuinely host-specific (provisioning rules, SMTP config) or belongs at a different architectural layer than this task's scope (`Advance.Platform` for security headers, a future `Advance.Auth`/IdP package for OpenIddict) per the split plan's own layering diagram. Don't fold them into SecurityCore just because they're "security-adjacent."
- **The dead `ScUser` (capital-S) scaffolding referenced in `Components/Account/Pages/Register.razor`.** Confirmed via the team's own memory notes ("ScUser ... copied boilerplate, not HRM gaps") and via reading the file — it constructs a `ScUser` and calls `DbContext.ScUsers.Add(...)`, a table/entity with zero relationship to the real `sc_user` this whole package is built around. Removed from the copy (see the file's own header comment for detail) rather than perpetuated into a new package.

---

## 8. Recommended staged migration order (real cutover — NOT this Phase-0 pass)

This is the **riskiest** of the three splits: every page in HRM depends on login/menu/rights working, and there is currently **no automated regression test** exercising the exact seam being cut (login → claims → menu-gated page → AD.CRUDManage-gated button). Recommend explicitly:

1. **Extract Password Policy + Security Alerts + MFA first.** These are additive and comparatively low-blast-radius: `PasswordPolicyService`/`SecurityAlertService`/`RequireMfaMiddleware` don't sit on the hot path of "can this user see anything at all" the way the claims factory does — a bug here degrades a secondary control, not the whole app. Regression pass: force-change-password still redirects correctly; a deliberately-failed login still counts toward lockout and alerts.
2. **Extract audit logging next** (`IAuditLogger`/`AuditLogger` — explicit-call-site logging only, per section 6's caveat that the automatic write-hook is NOT part of this). Regression pass: open a PDPA-flagged page, confirm an `AuditLog` row appears with the right actor/IP.
3. **Extract `ProgramRoleService`/menu seeding next** (AD.CRUDManage + `ScMenuNavSeeder`/`IMenuNavContributor`). This changes what gets seeded at startup but not (yet) what decides whether a request is authenticated at all. Regression pass: a page gated by `RequireAsync`/`GetRightsAsync` still shows/hides its Create/Edit/Delete buttons correctly for at least two different roles.
4. **The actual Identity/login/claims core LAST, and only after 1-3 are proven.** This is `ScUserClaimsPrincipalFactory`, `LoginEndpoints`, `MenuPolicyProvider`/`MenuAuthorizationHandler`, `IdentityRevalidatingAuthenticationStateProvider`, and the `AddIdentityCore`/`ConfigureApplicationCookie`/`SecurityDbContext` wiring itself — i.e. everything that, if subtly wrong, means **nobody can log in, or everybody who logs in gets the wrong menu.** Regression pass after this stage must include, at minimum: (a) a real login with a real password, (b) opening a page gated by `[Authorize(Policy="Menu:XXX")]` as a role that should and shouldn't see it, (c) an AD.CRUDManage-gated write action.

**Explicitly: stage 4 (and really, all of this) should NOT be executed by an unsupervised agent even in the real cutover.** A human should watch a real login succeed, end to end, after every single stage above — not just after the whole migration. The failure mode here is not "build breaks" (that's caught immediately); it's "build succeeds, login succeeds, but the wrong menu renders for one role" or "login silently stops attaching the `payroll_company` claim because `IHostClaimsEnricher` wasn't wired before the old code path was deleted" — the kind of thing that only shows up when a specific role's specific user tries a specific page, hours or days later.

---

## 9. Build results (deliverable #3)

`dotnet build -p:UseAppHost=false` run against each new `.csproj` individually (never against `HRM.csproj`/`HRM.sln`, per the rules) — **all four build clean, 0 errors**:

| Project | Result | Notes |
|---|---|---|
| `Advance.SecurityCore.Domain` | **Build succeeded** — 0 warnings, 0 errors | No fixes needed. |
| `Advance.SecurityCore.Data` | **Build succeeded** — 0 errors (NU1903 advisory warnings only, inherited transitively from `Microsoft.AspNetCore.Identity.EntityFrameworkCore`'s own dependency on `System.Security.Cryptography.Xml`, not introduced by this extraction) | No fixes needed. |
| `Advance.SecurityCore.Services` | **Build succeeded** — 0 errors | Two real bugs found and fixed during this pass: (1) `<FrameworkReference Include="Microsoft.AspNetCore.App" />` was miswritten inside a `<PropertyGroup>` instead of an `<ItemGroup>` — MSBuild silently ignores an unrecognized attribute there rather than failing on the csproj itself, so it surfaced as `HttpContext`/`RequestDelegate`/`PathString`/`IHttpContextAccessor`/`IServiceScopeFactory`/`ILogger<>` all failing to resolve. (2) Even after fixing that, those same types still didn't resolve — `Microsoft.NET.Sdk` (plain) does **not** carry Sdk.Web's implicit global-usings list the way HRM's actual `HRM.csproj` does, so files that compiled fine in HRM needed explicit `using Microsoft.AspNetCore.Http;` / `Microsoft.Extensions.DependencyInjection;` / `Microsoft.Extensions.Logging;` here. Fixed per-file plus a project-wide `GlobalUsings.cs` as a safety net. |
| `Advance.SecurityCore.Web` | **Build succeeded** — 0 errors, 31 warnings (all pre-existing in HRM's own copy of this scaffolding — `BL0008`/`CS0649` from the stock ASP.NET Core Identity template, not introduced here) | Same Sdk.Razor-vs-Sdk.Web implicit-usings gap as Services, at larger scale (~40 Razor files): missing `Microsoft.AspNetCore.Components.Forms`/`.Web`/`.Routing`/`Microsoft.AspNetCore.Authorization` broke every page using `EditForm`/`InputText`/`PageTitle`/`NavLink`/`[Authorize]` — fixed with one root `Components/_Imports.razor` (new file, not in HRM's original — HRM's pages inherit these from the app's own project-wide `_Imports.razor`, which is full of HRM-specific usings and wasn't copied). Three more one-off gaps: `ExternalLogins.LinkLoginCallbackAction` needed a `using` for the `.Manage` sub-namespace, `EnableAuthenticator.razor`'s QR code rendering needed the `QRCoder` package (present in HRM.csproj, not yet in this new csproj), `ResendConfirmation.razor`'s `HttpClient.PostAsJsonAsync` needed `System.Net.Http.Json`. All fixed; see each file's diff / this document's git history for exact locations. |

Net finding: **every fix needed here was mechanical (missing usings / a misplaced XML element / a missing package reference), not a design or logic problem** — no code changes were needed to any of the actual security logic (claims, menu policy, password policy, AD.CRUDManage, audit) to get a clean build. The `HostLayout`/`LayoutView` runtime substitution for the old `@layout HRM.Components.Layout.MainLayout` (see the Domain-copy-trims section / `Components/Account/Shared/AccountLayout.razor`) compiles but is **behaviorally untested** — no host app exists yet to supply a real layout and click through a live page.

---

## 10. Folder structure produced

```
src/Advance.SecurityCore/
  Advance.SecurityCore.Domain/       ApplicationUser, sc_user, sc_role, sc_user_role, sc_menu,
                                      sc_role_menu, sc_program_role, sc_program (added, see §5), AuditLog
  Advance.SecurityCore.Data/         SecurityDbContext (IdentityDbContext<ApplicationUser> + sc_* DbSets)
  Advance.SecurityCore.Services/     PasswordPolicy*, ProgramRoleService, ScProgramRouteSeeder,
                                      IMenuNavContributor + ScMenuNavSeeder, IHostClaimsEnricher,
                                      ScUserClaimsPrincipalFactory, MenuAuthorization,
                                      SecurityAlert*, Force PasswordChangeMiddleware, RequireMfaMiddleware,
                                      IAuditLogger/AuditLogger
  Advance.SecurityCore.Web/          LoginEndpoints, LdapAuthService (see §5), SecurityCoreOptions,
                                      ServiceCollectionExtensions (AddAdvanceSecurityCore),
                                      ApplicationBuilderExtensions (UseAdvanceSecurityCore),
                                      Components/Account/**  (copied wholesale, ~40 files, Identity
                                        scaffolding — Login/Register/2FA/Manage/etc.)
                                      Components/Shared/AccessDenied.razor
  EXTRACTION-PLAN.md                 this file
```
