# HRM.sln / HRM.csproj / Program.cs — mechanical wiring plan for the 4-way split

Read against `docs/Plan_Split_Payroll_Workflow_v1.1.md` (v1.1, 12 ก.ย. 2569) and the real
`HRM.sln`, `HRM.csproj`, `Program.cs` as they stand on branch `develop` at commit `e9352bf`
(12 ก.ย. 2569). This is a wiring/mechanics document only — it does not re-litigate the plan's
module boundaries, only says exactly which lines move, in what order, and what the new lines
look like.

**Scope note on naming**: the task that produced this document names the four sibling projects
`src/Advance.Workflow/`, `src/Advance.Payroll/`, `src/Advance.SecurityCore/`, `src/Advance.Host/`.
The plan document itself only names three of those exactly — `Advance.Workflow`, `Advance.Payroll`,
and `Advance.Host` (section 3.2: "สร้าง `Advance.Host` (shell)..."). It does **not** use the name
`Advance.SecurityCore` anywhere; instead it describes the same territory split across two *future*
level-0/1 packages, `Advance.Platform` (audit, access log, security headers) and `Advance.Auth`
(login/SSO — explicitly "ยังไม่มี", not built yet, HumanOk keeps its own Identity for now). Section 4
does not schedule either of those as a phase-0..4 deliverable at all. Treat `Advance.SecurityCore`
in this document as **that same Platform+Auth territory under a different working name** chosen by
whoever is scaffolding it — confirm the name-to-scope mapping against the real
`src/Advance.SecurityCore/*.csproj` once it exists, because if it turns out to be scoped narrower
(e.g. just audit/PDPA, with Identity staying in HRM/Host) several of the Program.cs line ranges
below (the `AddIdentityCore` block especially) would not move at all in the real cutover.

---

## 1. HRM.sln

### 1.1 What's there today (quoted verbatim)

```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "HRM", "HRM.csproj", "{01A9237B-8302-407B-8A31-166BB7E3E293}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "HRM.Tests", "HRM.Tests\HRM.Tests.csproj", "{6F5C44C6-CE70-436C-B2D8-9CB149E6A055}"
EndProject
```

Shape: `Project("{<project-type-GUID>}") = "<name>", "<relative path to .csproj>", "{<project instance GUID>}"` /
`EndProject`. Note the two projects already use **different** project-type GUIDs even though both
are ordinary SDK-style C# projects — `9A19103F-16F7-4668-BE54-9A1E7A4F7556` for the `Sdk="Microsoft.NET.Sdk.Web"`
executable (HRM.csproj) and `FAE04EC0-301F-11D3-BF4B-00C04F79EFBC` for the test project
(HRM.Tests.csproj, presumably `Sdk="Microsoft.NET.Sdk"`). This matters for the worked examples
below: **`dotnet sln add`** is what will actually generate these blocks during the real cutover
(nobody should hand-type project-type GUIDs), and it picks the GUID based on the target
`<Sdk>` value/output type it finds in the referenced `.csproj`, not on which solution it's being
added to. So:
- a `Microsoft.NET.Sdk.Web` project (any `*.Web` host, e.g. `Advance.Host.Web`) → `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`
- a plain `Microsoft.NET.Sdk`/`Microsoft.NET.Sdk.Razor` class library (Domain/Data/Engine/Contracts/Blazor RCL projects) → `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`

`GlobalSection(SolutionConfigurationPlatforms)` (lines 11–18, quoted verbatim):

```
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
```

**Correction to the task premise**: this section does *not* need new entries per project. It
declares the solution-wide configuration/platform combinations that exist (Debug/Release ×
Any CPU/x64/x86) — it is not per-project. Every new project will reuse these same six combos
unless someone deliberately introduces a configuration name the solution doesn't already have
(there is no reason to for this split). Leave this section untouched.

`GlobalSection(ProjectConfigurationPlatforms)` (lines 19–44) **does** need 12 new lines per new
project instance GUID — quoted verbatim for the existing HRM.csproj GUID as the pattern:

```
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|x64.ActiveCfg = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|x64.Build.0 = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|x86.ActiveCfg = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Debug|x86.Build.0 = Debug|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|Any CPU.Build.0 = Release|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|x64.ActiveCfg = Release|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|x64.Build.0 = Release|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|x86.ActiveCfg = Release|Any CPU
		{01A9237B-8302-407B-8A31-166BB7E3E293}.Release|x86.Build.0 = Release|Any CPU
```

Note every `x64`/`x86` row maps `ActiveCfg`/`Build.0` back to the `Any CPU` build (there is no
real per-platform build happening) — every new project block must follow that same
"collapse everything onto Any CPU" pattern, not introduce real x86/x64 builds.

### 1.2 Parametric rule for the 4 new areas

**For each `.csproj` found under `src/Advance.Workflow/**`, `src/Advance.Payroll/**`,
`src/Advance.SecurityCore/**`, `src/Advance.Host/**`:**

1. Add one `Project(...)/EndProject` block (§1.1 shape) right after the existing two projects
   (before the `Global` line), using a **freshly generated GUID** for the project instance
   (never reuse one — `dotnet sln add` does this for you; if hand-editing, generate with
   `[guid]::NewGuid()` in PowerShell or `New-Guid`, never copy a placeholder).
2. Add the matching 12-line `ProjectConfigurationPlatforms` block (§1.1 shape) for that same
   instance GUID.
3. Do **not** touch `SolutionConfigurationPlatforms`.

Run `dotnet sln HRM.sln add <path-to-csproj>` per project instead of hand-editing where possible —
it does exactly the above and removes the chance of a copy-paste GUID collision.

### 1.3 One worked example per area (concrete GUIDs generated for this document — real, not placeholders)

**Advance.Workflow** — example project `Advance.Workflow.Engine.csproj` (plain class library):

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Advance.Workflow.Engine", "src\Advance.Workflow\Advance.Workflow.Engine\Advance.Workflow.Engine.csproj", "{920225B2-4848-4D25-A8F8-4587B519C036}"
EndProject
```
ProjectConfigurationPlatforms addition:
```
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|x64.ActiveCfg = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|x64.Build.0 = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|x86.ActiveCfg = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Debug|x86.Build.0 = Debug|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|Any CPU.Build.0 = Release|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|x64.ActiveCfg = Release|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|x64.Build.0 = Release|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|x86.ActiveCfg = Release|Any CPU
		{920225B2-4848-4D25-A8F8-4587B519C036}.Release|x86.Build.0 = Release|Any CPU
```
Per the plan's §3 diagram, Advance.Workflow is actually **6** projects (Contracts, Org, Domain,
Data, Engine, Blazor) — repeat the block above once per `.csproj` actually found on disk, each
with its own fresh GUID. Do not assume exactly 6; count what's really there.

**Advance.Payroll** — example project `Advance.Payroll.Engine.csproj`:
```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Advance.Payroll.Engine", "src\Advance.Payroll\Advance.Payroll.Engine\Advance.Payroll.Engine.csproj", "{1967269D-A0EB-41F8-8C39-833BEC5CA1E3}"
EndProject
```
(+ the matching 12-line block using GUID `1967269D-A0EB-41F8-8C39-833BEC5CA1E3`.) Plan §3 lists
7 projects for Payroll (Core, Domain, Data, Engine, Reports, Blazor, Web) — same "repeat per
real `.csproj`" rule.

**Advance.SecurityCore** — example project `Advance.SecurityCore.Domain.csproj`:
```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Advance.SecurityCore.Domain", "src\Advance.SecurityCore\Advance.SecurityCore.Domain\Advance.SecurityCore.Domain.csproj", "{3F2C444D-968D-4728-9612-0D6516469F83}"
EndProject
```
(+ matching block, GUID `3F2C444D-968D-4728-9612-0D6516469F83`.) Project count here is whatever
the scaffolding agent chose (unknown from this worktree — see the naming caveat at the top);
follow the `X.Domain/X.Data/X.Web` shape from `ADVANCE-ARCHITECTURE.md` as the default guess.

**Advance.Host** — example project `Advance.Host.Web.csproj` (this one IS an executable, so it
gets the *other* project-type GUID):
```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Advance.Host.Web", "src\Advance.Host\Advance.Host.Web\Advance.Host.Web.csproj", "{D2C5BBCF-703E-455E-999B-D18979DE58DD}"
EndProject
```
(+ matching block, GUID `D2C5BBCF-703E-455E-999B-D18979DE58DD`.)

---

## 2. HRM.csproj

### 2.1 Every `<PackageReference>` currently present, classified

Quoting the two real `PropertyGroup`/`ItemGroup` package blocks (lines 3-8, 10-53) and
classifying each package: **stays in HRM** (keep as-is), **new project needs own copy** (any
project that compiles types against it must list it directly — PackageReference is transitive
through ProjectReference for *runtime*, but a Razor Class Library or any project that
references the package's types at *compile* time needs its own `<PackageReference>`), or
**drop candidate** (grep found no real usage — don't carry it anywhere without confirming first).

Grep against the live source (not guesswork) found these concrete usage sites, which is what the
classification below is grounded on:

| Package | Where it's actually used (grep-verified) | Classification |
|---|---|---|
| `ClosedXML` 0.105.1 | `Services\Reporting\Export\ExcelReportExporter.cs` (generic Reporting framework) only | **Stays in HRM.** Not currently used by any Pay_*/wf_*/security code — do not add to Payroll/Workflow/SecurityCore unless one of them grows its own Excel export outside the shared Reporting framework. |
| `DocumentFormat.OpenXml` 3.5.1 | `Services\Reporting\Export\WordReportExporter.cs`, `ExcelReportExporter.cs` (Reporting framework) | **Stays in HRM.** Same reasoning as ClosedXML. |
| `MailKit` 4.16.0 | `EmailSender` (general notification, referenced from Program.cs `AddTransient<IEmailSender, EmailSender>`) | **Stays in HRM.** `Advance.Host` needs its own copy (a standalone Payroll/Workflow product still needs to send email — payslip delivery, approval notifications — without HRM present); Workflow.Engine/Payroll.Engine themselves should depend only on `IWorkflowNotifier`/an email abstraction, not MailKit directly. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0 | `Program.cs` `"ExternalApi"` JWT scheme (ecosystem/chatbot resource server) | **Stays in HRM.** `Advance.SecurityCore`/`Advance.Host` need their own copy once Auth actually moves (not in this wave per the plan — HumanOk keeps its own Identity for now). |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.0 | `Program.cs` AD/SSO scaffold loop (`ExternalAuth:Sso:Providers`) | **Stays in HRM.** Same as above — needed by whichever project ends up owning the login/SSO shell (`Advance.Host` per plan §3.2). |
| `Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter` 10.0.10 | Blazor list/grid pages generally | **Stays in HRM; every new Blazor RCL needs its own copy** — `Advance.Workflow.Blazor`, `Advance.Payroll.Blazor`, `Advance.Host.Web` (any project that compiles `.razor` files using `QuickGrid` bound to an `IQueryable` from EF). |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` 10.0.10 | Dev-time EF error pages (`app.UseMigrationsEndPoint()`/dev exception filter) | **Stays in HRM/Advance.Host only** (the actual executable Web projects). Class libraries (Domain/Data/Engine/Contracts) never need this. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.0 | `Program.cs` `AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>()` | **Stays in HRM.** `Advance.SecurityCore`/`Advance.Host` need their own copy once/if Identity actually moves — flagged as **not scheduled in this wave** per the plan (§3: "Advance.Auth — ยังไม่มี"). |
| `Microsoft.AspNetCore.Identity.UI` 10.0.0 | Scaffolded Identity `/Account` Razor components (`MapAdditionalIdentityEndpoints()`) | Same as above — stays in HRM; `Advance.Host` needs its own copy for standalone products' login UI. |
| `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 | Not grep-confirmed against a specific service; likely a test/dev-only provider | **Stays in HRM** as-is; not a candidate to copy into new projects unless a specific Data project wants a file-based dev DB. Flag for confirmation — grep shows no direct `UseSqlite(` call in Program.cs or Services; if truly unused, it's a drop candidate, but that's outside this task's read-only/no-changes scope. |
| `Microsoft.EntityFrameworkCore.SqlServer` 10.0.0 | Every `HRMContext`/`ApplicationDbContext` `UseSqlServer(...)` call | **Every new `*.Data` project needs its own copy** — `Advance.Workflow.Data`, `Advance.Payroll.Data`, `Advance.SecurityCore.Data` each get their own `DbContext` per the plan (§3: "Advance.Workflow.Data (WorkflowDbContext + migrations ของตัวเอง)", "Payroll.Data PayrollDbContext"). HRM keeps its own copy for `HRMContext`/`ApplicationDbContext`. |
| `Microsoft.EntityFrameworkCore.Tools` 10.0.0 | Dev-time `dotnet ef migrations add` tooling | **Every new `*.Data` project needs its own copy** — each will run its own `dotnet ef migrations add` against its own DbContext (per this repo's own `CLAUDE.md` migrations convention, hand-authored per project going forward). |
| `Microsoft.ML` 5.0.0 / `Microsoft.ML.TimeSeries` 5.0.0 | grep-confirmed in **both** `Services\Job\JdCompetencySuggestionService.cs` (Rec/Talent, stays HRM) **and** `Services\Payroll\PayrollAnalysisService.cs` + `Services\Pay\Calculators\PayrollSpikeDetector.cs` (Pay_* anomaly detection — the `MLModels\*_spike_model.zip` files in the csproj's last `<ItemGroup>` are these same models) | **HRM keeps its own copy** (JD competency suggestion is not part of the split). **`Advance.Payroll` needs its own copy** for `PayrollSpikeDetector`/`PayrollAnomalyDetectionService` — and the 4 `MLModels\*.zip` `<None Update>` entries (deductions/other_income/overtime/salary spike models) move with it. |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` 10.0.0-rc.1 | Dev-time scaffolding tool (`dotnet aspnet-codegenerator`) | **Stays in HRM only.** Never needed by a class library; not needed by `Advance.Host` unless someone scaffolds CRUD pages there with the same tool (harmless either way, dev-only). |
| `MudBlazor` 8.14.0 | The overwhelming majority of `.razor` pages app-wide | **Every Blazor RCL/Web project needs its own copy** — `Advance.Workflow.Blazor`, `Advance.Payroll.Blazor`, `Advance.Host.Web` all compile `.razor` files that reference `Mud*` components directly, so each needs the `PackageReference`, not just a transitive one via `ProjectReference`. **Pin the exact same version (`8.14.0`)** everywhere — see Risk #1 below. |
| `OpenIddict.AspNetCore` 7.6.0 / `OpenIddict.EntityFrameworkCore` 7.6.0 | `Program.cs` — HRM as OIDC IdP for ERP SSO | **Stays in HRM/Advance.Host.** Not needed by Workflow/Payroll/SecurityCore engine or domain libraries — they never issue tokens directly. |
| `PdfPig` 0.1.16 | `Services\Rec\CvParsing.cs` (CV parser, e-recruit) | **Stays in HRM only.** Not part of the split (Rec_* isn't Payroll or Workflow). |
| `QRCoder` 1.8.0 | grep-confirmed in `Components\Pages\Att\GeofenceLocationAdmin.razor` (Att_* GPS check-in QR) **and** `Components\Account\Pages\Manage\EnableAuthenticator.razor` (2FA QR) | **HRM keeps its own copy** (Att_* check-in). **`Advance.SecurityCore` needs its own copy** for 2FA enrollment if/when `EnableAuthenticator` moves with Identity — **not** a Payroll dependency today despite the plan doc's "สลิป PDF ใส่รหัส" line; that phrase was verified against the plan and Program.cs to mean a printed reference code, not a QR — don't add QRCoder to `Advance.Payroll` speculatively. |
| `QuestPDF` 2026.7.2 | `PayslipGenerationService` (payslip PDFs) and the shared `PdfReportExporter` (Reporting framework) | **HRM keeps its own copy** (non-payroll PDF reports). **`Advance.Payroll.Reports` needs its own copy** — this is the one example the task description already named, and it's grep-confirmed correct: payslip generation is squarely Payroll's. |
| `Radzen.Blazor` 8.3.0 | grep-confirmed **only** in `Components\Pages\Payroll\PayrollProcess - Copy.razor` and `Components\Pages\Payroll\HRIncome.razor` — both **legacy Payroll pages** (the " - Copy" filename and `HRIncome.razor` match the 13 legacy Payroll pages the plan's Phase 0 item 5 already schedules to move into a `Legacy/` folder and hide from the menu) | **Likely a drop candidate for the new `Advance.Payroll.Blazor` RCL**, not an automatic carry-over — the *new* Pay_* engine's 41+2 pages use MudBlazor, not Radzen. Confirm which pages actually ship in Payroll's real page count before deciding whether Radzen needs to travel at all; do not reflexively add it to Payroll.Blazor just because "Radzen usage" and "Payroll" both showed up in the same grep. |
| `SendGrid` 9.29.3 | Alternate email provider alongside MailKit | Same treatment as MailKit: **stays in HRM; `Advance.Host` needs its own copy.** |
| `Serilog` / `Serilog.AspNetCore` / `Serilog.Extensions.Logging` / `Serilog.Sinks.Console` / `Serilog.Sinks.File` | `Program.cs` `Log.Logger = new LoggerConfiguration()...` — configured **once**, in the executable's `Program.cs` | **Stays in HRM/Advance.Host only.** `Advance.Workflow.*`, `Advance.Payroll.*`, `Advance.SecurityCore.*` class libraries must log via the standard `Microsoft.Extensions.Logging.ILogger<T>` abstraction (already part of the shared framework, no package needed) and never take a direct Serilog dependency — only whichever project actually calls `UseSerilog()`/configures sinks needs these 5 packages. |
| `System.DirectoryServices.Protocols` 9.0.0 | `HRM.Services.Auth.LdapAuthService` (AD/LDAP bind) | **Stays in HRM.** `Advance.SecurityCore` needs its own copy if/when LDAP auth moves with Identity (not scheduled this wave). |
| `Tesseract` 5.2.0 | CV parser OCR | **Stays in HRM only.** Not part of the split. |
| `Microsoft.Build` 17.11.48 / `NuGet.Protocol` 6.12.5 | **grep found zero usage anywhere in the codebase outside the `.csproj` declaration itself** (`PrivateAssets=all` on both — build-time-only, no `using Microsoft.Build`/`using NuGet.Protocol` anywhere in `Services`/`Components`/`Endpoints`) | **Flagged, not classified as "needed" anywhere.** These look vestigial (possibly left over from a scaffolding/codegen tool run, or a dead in-house version-check helper). Don't copy them into any new project on the assumption they're load-bearing — confirm with a full-solution search (this task is read-only, so that confirmation is left to whoever does the real cutover) before deciding whether to drop them entirely. |
| `SQLitePCLRaw.lib.e_sqlite3` (pre-release) | Native provider paired with `Microsoft.EntityFrameworkCore.Sqlite` above | Same fate as the Sqlite package above — stays in HRM, not a copy candidate. |

### 2.2 The two `<Folder Include>`/`<Content>`/`<Watch>` ItemGroups (lines 55-60, 62-66, 68-79, 81-85, 87-100)

These are HRM-specific build plumbing (bundled Tesseract language data, excluding
`HRM.Tests\**` from the web project's compile glob, watch excludes for `logs\`/`wwwroot\uploads\`,
one `Content Update` for a single-file-deploy flag, and the 4 `MLModels\*.zip` `CopyToOutputDirectory`
entries). None of this is package-reference material, so it isn't part of the classification table,
but two items travel with the code they belong to:
- The 4 `<None Update="MLModels\*_spike_model.zip">` entries move to `Advance.Payroll` (whichever
  project hosts `PayrollSpikeDetector`) alongside `Microsoft.ML`/`Microsoft.ML.TimeSeries` above.
- The `<Compile Remove="HRM.Tests\**\*.cs" />` / `<Content Remove="HRM.Tests\**\*" />` /
  `<None Remove="HRM.Tests\**\*" />` triplet is purely about HRM.Tests living inside HRM's own
  directory tree — irrelevant to any new project laid out under `src/`.

### 2.3 `<ProjectReference>` lines HRM.csproj needs once it depends on the 4 new projects

HRM.csproj today has **zero** `<ProjectReference>` elements (confirmed — the file has no
`ItemGroup` containing one). Once Phase 1/2 land, add one `<ProjectReference>` per new project HRM
actually consumes directly. Per the plan's own dependency rule ("HumanOk อ้าง Payroll/Workflow ได้"),
HRM references the **Engine + Blazor** faces of each package, not their Domain/Data internals
directly where a cleaner seam exists — but since HRM's own `HRMContext` is what runs migrations
today and both new packages initially still run inside `HRM.sln` (plan §4, Phase 1/2: "อยู่ใน
`HRM.sln` เดิม (ProjectReference)"), HRM also needs the Data projects referenced transitively at
minimum for `dotnet ef database update` orchestration during the shared-solution phase:

```xml
<ItemGroup>
  <!-- Advance.Workflow (Phase 1) -->
  <ProjectReference Include="..\src\Advance.Workflow\Advance.Workflow.Contracts\Advance.Workflow.Contracts.csproj" />
  <ProjectReference Include="..\src\Advance.Workflow\Advance.Workflow.Engine\Advance.Workflow.Engine.csproj" />
  <ProjectReference Include="..\src\Advance.Workflow\Advance.Workflow.Blazor\Advance.Workflow.Blazor.csproj" />

  <!-- Advance.Payroll (Phase 2) -->
  <ProjectReference Include="..\src\Advance.Payroll\Advance.Payroll.Engine\Advance.Payroll.Engine.csproj" />
  <ProjectReference Include="..\src\Advance.Payroll\Advance.Payroll.Reports\Advance.Payroll.Reports.csproj" />
  <ProjectReference Include="..\src\Advance.Payroll\Advance.Payroll.Blazor\Advance.Payroll.Blazor.csproj" />

  <!-- Advance.SecurityCore (only if/when Identity actually moves — not scheduled in Phases 0-4 above) -->
  <!-- <ProjectReference Include="..\src\Advance.SecurityCore\Advance.SecurityCore.Domain\Advance.SecurityCore.Domain.csproj" /> -->
</ItemGroup>
```

Note the path is `..\src\...` because HRM.csproj lives at the repo root (`D:\GitWorkspace\HRM\HRM.csproj`)
and the new projects live under `src\` — one level *down*, not up, from the repo root, so the
correct relative prefix from HRM.csproj is actually `src\Advance.Workflow\...` (no `..\`) —
**correct this before use**: HRM.csproj and `src\` share the same parent (`D:\GitWorkspace\HRM\`),
so the real relative path is:

```xml
<ProjectReference Include="src\Advance.Workflow\Advance.Workflow.Engine\Advance.Workflow.Engine.csproj" />
```

`Advance.Host` is **not** referenced by HRM.csproj at all — per §3.2 of the plan, `Advance.Host`
is the shell that `Advance.Payroll.Web` and `Advance.Workflow.Web` (the new standalone products)
build on; HRM remains its own full host and does not consume it. See §3's Advance.Host row for
the fuller explanation of why there's no corresponding removal in Program.cs either.

---

## 3. Program.cs — DI/middleware lines removed per concern, and their replacement

`Program.cs` is 959 lines. Line numbers below are exact against the copy read for this document.

### 3.1 Advance.Workflow

Cleanly delimited already in the DI section — but two related registrations sit just *outside*
the labeled block and are easy to miss:

| Line(s) | Content | Action |
|---|---|---|
| 485 | `builder.Services.AddScoped<HRM.Services.Workflow.WorkflowEngineService>();` | **Remove.** Per the plan and this repo's own commit history (`30a50ea Workflow: retire the old engine — one engine, one code path`), this is already the retired-to-facade old engine per Phase 0 item 3 — confirm on the real cutover branch whether this line (and the class) still exists at all by that point, since it may already be deleted. |
| 486 | `builder.Services.AddScoped<HRM.Services.Workflow.WorkflowService>();` | **Remove** → replaced by the package's own registration call. |
| 487 | `builder.Services.AddScoped<HRM.Services.Workflow.WorkflowStateChangeService>();` | **Remove**, folded into the same replacement call. |
| 488 | `builder.Services.AddScoped<HRM.Services.Workflow.WorkflowButtonService>();` | **Remove**, folded into the same replacement call. |
| 490 | `HRM.Services.Workflow.WorkflowDocumentHandlers.AddWorkflowDocumentHandlers(builder.Services);` | **Stays in HRM**, does *not* move — per plan §3 ("17 handler เขียนกลับเอกสารเมื่อปิดงาน... **อยู่กับโมดูลเจ้าของ ไม่ย้ายไปกับ engine** — engine รู้จักแค่ interface `IWorkflowDocumentHandler`"). This line registers HRM's own `IWorkflowDocumentHandler` implementations against the engine's DI container — it must run *after* the new `AddAdvanceWorkflow(...)` call below, not instead of it. |
| 792 | `app.MapWorkflowFileEndpoints();` | **Confirm before moving** — `Endpoints\WorkflowFileEndpoints.cs` needs to be read in the real cutover to check whether it serves `doc_center` (generic, HRM-owned per this repo's `CLAUDE.md`) or a `wf_*`-specific attachment table. Do not move this line blindly with the rest of the Workflow block. |
| 935 | `await HRM.Services.Workflow.WorkflowStateChangeSeeder.EnsureAsync(app.Services);` | **Remove** → replaced by a call the package exposes for its own idempotent seeding (workflow-state-change-request workflow is `wf_*`-domain-owned). |

Proposed replacement (name/signature not yet scaffolded anywhere on disk in this worktree —
`src/Advance.Workflow` does not exist here; **confirm against the real scaffold once available**):

```csharp
// replaces lines 485-488
builder.Services.AddAdvanceWorkflow(builder.Configuration);
```
and, after `var app = builder.Build();`, in place of line 935:
```csharp
await app.Services.SeedAdvanceWorkflowAsync(); // or app.UseAdvanceWorkflow() if it registers middleware too
```

No workflow-specific `app.Use...()` middleware exists in today's pipeline (the engine is called
from page code, not middleware) — so unlike SecurityCore below, there is likely no
`app.UseAdvanceWorkflow()` needed, only the DI registration + seeder call. Confirm this remains
true once `IOrgDirectorySource`/`IWorkflowNotifier` wiring (plan §3, Phase 1) is added — those are
new DI registrations HRM will need to add (not remove), implementing the package's interfaces:

```csharp
builder.Services.AddScoped<IOrgDirectorySource, HRM.Services.Workflow.HrOrgDirectorySource>(); // HRM-side adapter, new
builder.Services.AddScoped<IWorkflowNotifier, HRM.Services.Workflow.EmailWorkflowNotifier>();   // HRM-side adapter, new
```

### 3.2 Advance.Payroll

The Pay_* block is cleanly delimited by the repo's own comment markers — **but contains one
mis-filed pair that must NOT move with it**:

| Line(s) | Content | Action |
|---|---|---|
| 453 | `builder.Services.AddScoped<HRM.Services.Payroll.PayrollCalculationService>();` (legacy, `HRM.Services.Payroll` namespace) | **Remove**, but this is the *old* legacy Payroll (13 hidden legacy pages), not the Pay_* engine — confirm with whoever is finishing Phase 0 item 5 whether it's deleted outright rather than moved. |
| 454 | `builder.Services.AddSingleton<PayrollAnalysisService>();` (legacy) | Same as above — legacy, likely deleted not moved. |
| 456 | `// ----- Pay_* module (new payroll engine, parallel to the legacy Payroll pages) -----` | Start of the real block to extract. |
| 457-476 | `AddScoped<ISocialSecurityRateProvider, HrucfsecurityRateProvider>()` through `AddScoped<HRM.Services.Pay.EmployeeRehireService>()` **plus** `AddScoped<HRM.Services.Audit.IAuditLogger, HRM.Services.Audit.AuditLogger>()` | **Remove all except the `IAuditLogger`/`AuditLogger` line** → replaced by the package's registration call. |
| 476 | `builder.Services.AddScoped<HRM.Services.Audit.IAuditLogger, HRM.Services.Audit.AuditLogger>();` | **Do NOT move to Payroll.** This line sits inside the `# Pay_* module` comment block but `IAuditLogger`/`AuditLogger` is a general-purpose audit service (per this repo's own `CLAUDE.md`: "`Model/HRMContext.Audit.cs`... logs every Create/Update/Delete on every entity"; `IAuditLogger.LogAccessAsync` is called from PDPA-badge pages across every module, not just Pay_*). A copy-paste extraction that blindly follows the comment markers would wrongly move general audit logging into Payroll. This belongs with `Advance.SecurityCore` (or stays in HRM if SecurityCore's real scope turns out narrower — see the naming caveat at the top of this document). |
| 478-482 | `AuditArchiveOptions` config bind + `builder.Services.AddScoped<HRM.Services.Audit.AuditArchiveService>();` | **Same flag as line 476** — general AuditLog archiving, not Payroll-specific, mis-filed inside the Pay_* comment block. Goes with SecurityCore, not Payroll. |
| 483 | `// ----- end Pay_* module -----` | End of block. |
| 603-607 | `PayDateService`, `PayrollCalcJobRegistry`, `PayrollCalcJobService`, `PayrollPreflightService` (Phase A/B payroll, outside the labeled block, interleaved with Idp_* module registrations) | **Remove**, folded into the package's registration call — these are genuinely payroll (pay-date default, background calc job, pre-flight validation) despite being physically placed among unrelated Idp_* lines. |
| 775 | `app.MapPayrollFileEndpoints();` | **Remove** → replaced by the package's own endpoint-mapping extension. |

Proposed replacement (same "propose + flag" caveat as Workflow — nothing scaffolded yet in this
worktree):

```csharp
// replaces lines 457-475, 603-607
builder.Services.AddAdvancePayroll(builder.Configuration);
```
```csharp
// replaces line 775
app.MapAdvancePayrollFileEndpoints();
```

HRM must also register its 5 feed-interface implementations here (new lines, not replacements —
per plan §3 "HRM.Adapters: `IEmployeeSource`, `IAttendanceFeed`, `IOvertimeFeed`, `ILoanFeed`,
`IAllowanceFeed`, `IApproverResolver`, `IWorkflowUserDirectory`"):

```csharp
builder.Services.AddScoped<IEmployeeSource, HRM.Services.Pay.HremployeeSource>();
builder.Services.AddScoped<IAttendanceFeed, HRM.Services.Pay.AttDailyAttendanceFeed>();
builder.Services.AddScoped<IOvertimeFeed, HRM.Services.Pay.HrwOtFeed>();
builder.Services.AddScoped<ILoanFeed, HRM.Services.Pay.EmployeeLoanFeed>();
builder.Services.AddScoped<IAllowanceFeed, HRM.Services.Pay.WelfareAllowanceFeed>();
```

`Microsoft.ML`/`Microsoft.ML.TimeSeries` PayrollSpikeDetector registration is folded inside
`AddAdvancePayroll(...)` in the package, not a separate HRM line.

### 3.3 Advance.SecurityCore

This is the **least mechanical** of the four — the registrations are scattered across the whole
file rather than delimited by a comment block, several are entangled with `ApplicationDbContext`
(which is not moving in this wave), and — per the naming caveat at the top — it is not certain
this exact set is what the real `Advance.SecurityCore` scaffold will actually claim.

| Line(s) | Content | Action |
|---|---|---|
| 181-183 | `Configure<SecurityAlertOptions>(...)`, `AddSingleton<SecurityAlertCounter>()` | **Remove** → folded into replacement. |
| 184-186 | `EFilingFormats.SsoPrefixCodeMale/Female = ...` | **Does not move** — this is Payroll e-Filing config (สปส.1-10 prefix codes), mis-adjacent to the security block only by physical proximity in the file. Leave with Payroll wiring, not SecurityCore. |
| 187 | `AddSingleton<SecurityAlertService>();` | **Remove** → folded into replacement. |
| 188-193 | `Configure<PasswordPolicyOptions>(...)`, `AddSingleton<PasswordPolicyService>()`, `var passwordPolicy = ...Get<PasswordPolicyOptions>()` | **Remove**, but `passwordPolicy` is a local variable consumed a few lines later inside the `AddIdentityCore` lambda (line 211-212) — the replacement must either still expose the resolved options via DI (`IOptions<PasswordPolicyOptions>`) for that lambda to read, or the whole `AddIdentityCore` block has to move too (see next row). This is the sharpest "don't extract this snippet in isolation" trap in the whole file. |
| 195-221 | `AddIdentityCore<ApplicationUser>(...).AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager().AddDefaultTokenProviders().AddClaimsPrincipalFactory<ScUserClaimsPrincipalFactory>()` | **Flag, do not mechanically move in this wave.** This is deeply coupled to `ApplicationDbContext` (an HRM/`HRM.Data` type) and `ScUserClaimsPrincipalFactory` (bridges `sc_user` legacy rows to claims). The plan explicitly defers Identity/Auth extraction ("Advance.Auth — ยังไม่มี... HumanOk ใช้ Identity เดิม"). Leave this block in HRM/Program.cs untouched until a real `Advance.Auth`/SecurityCore.Auth scaffold exists with its own DbContext story. |
| 240-250 | `ConfigureApplicationCookie(...)` | Same as above — tied to the same Identity setup, does not move this wave. |
| 350-351 | `AddScoped<LdapAuthService>()`, `AddScoped<ExternalIdentityProvisioningService>()` | **Flag, likely moves with Identity later, not this wave** — same dependency chain. |
| 356-357 | `AddMemoryCache()`, `AddScoped<ProgramRoleService>()` | **Remove** → folded into replacement (AD.CRUDManage is a clean, self-contained mechanism per this repo's `CLAUDE.md` — table reads behind a memory cache, no DbContext coupling beyond `HRMContext`/whatever context each app uses for `sc_program_role`). |
| 374-375 | `AddSingleton<IAuthorizationPolicyProvider, MenuPolicyProvider>()`, `AddSingleton<IAuthorizationHandler, MenuAuthorizationHandler>()` | **Flag** — `MenuPolicyProvider` resolves against `sc_menu`/`sc_role_menu`, which are HRM-legacy tables; a standalone Payroll/Workflow product needs its *own* menu/rights tables per the shell (`Advance.Host`), not these exact classes. Don't lift verbatim — this is the pattern `Advance.Host`'s equivalent should be modeled on (see §3.4). |
| 381 | `AddSingleton<IAuthorizationHandler, ProgramAuthorizationHandler>();` | Per this repo's own `CLAUDE.md`, `ProgramAuthorizationHandler`/`ProgramAuthorization.cs` is "a superseded proof-of-concept — don't extend it to new pages." **Do not carry this into any new project.** |
| 392-400 | `AddAuthorizationCore(options => options.AddPolicy("ExternalApiCaller", ...))`, `AddSingleton<IAuthorizationHandler, ExternalApiCallerHandler>();` | **Stays in HRM** — this is specifically the ecosystem/chatbot resource-server surface for *this* app; not generic enough to lift into SecurityCore as-is. |
| 656-688 | `AddRateLimiter(options => { AddSlidingWindowLimiter("login", ...); AddSlidingWindowLimiter("career-apply", ...); AddSlidingWindowLimiter("forgot-password", ...); options.OnRejected = ...})` | **Remove the generic rate-limiter setup + `"login"` policy** → folded into replacement (rate-limiting login attempts is a generic security concern). **`"career-apply"` and `"forgot-password"` policies stay in HRM** — they're specific to HRM's own recruitment/self-service endpoints, not generic enough for SecurityCore. |
| 722 | `app.UseMiddleware<HRM.Middleware.SecurityHeadersMiddleware>();` (`Middleware\SecurityHeadersMiddleware.cs`) | **Remove** → `app.UseAdvanceSecurityCore();` (headers). |
| 758 | `app.UseMiddleware<HRM.Middleware.ForcePasswordChangeMiddleware>();` (`Middleware\ForcePasswordChangeMiddleware.cs`) | **Remove** → folded into `app.UseAdvanceSecurityCore()` (or a second explicit call if the package separates concerns — confirm against scaffold). |
| 760 | `app.UseMiddleware<HRM.Middleware.RequireMfaMiddleware>();` (`Middleware\RequireMfaMiddleware.cs`) | **Remove** → same as above. |
| 762 | `app.UseRateLimiter();` | **Stays** (applies to both the moved `"login"` policy and HRM's own remaining policies — the call itself doesn't move, only the policy *definitions* above it partially move). |
| 911-912 | `await ScProgramRouteSeeder.SeedAsync(app.Services); await ProgramRoleService.SeedAsync(app.Services);` | **Remove** → `await app.Services.SeedAdvanceSecurityCoreAsync();` |

Proposed replacement (unscaffolded — confirm signatures once `src/Advance.SecurityCore` exists):

```csharp
// replaces lines 181-183, 187-193 (options/services), 356-357, 374-375 DI portion
builder.Services.AddAdvanceSecurityCore(builder.Configuration);
```
```csharp
// replaces lines 722, 758, 760
app.UseAdvanceSecurityCore();
```
```csharp
// replaces lines 911-912
await app.Services.SeedAdvanceSecurityCoreAsync();
```

**Given the size of the "flag, does not move this wave" rows above (Identity/cookie/LDAP —
roughly half of what a reader would naively call "security" in this file), the honest scope of
what SecurityCore's Program.cs cutover removes in this wave is small**: alert options/service,
password-policy *options binding* (not the Identity lambda that reads it), AD.CRUDManage
(ProgramRoleService + seeders), the 3 security middleware, and the generic slice of rate
limiting. Everything Identity-shaped stays in HRM until `Advance.Auth` is real.

### 3.4 Advance.Host

**No lines are removed from HRM's `Program.cs` for this concern.** Per plan §3.2, `Advance.Host`
is shell infrastructure that the two *new standalone products* (`Advance.Payroll.Web`,
`Advance.Workflow.Web`) build on — HRM itself remains a complete, self-hosting monolith and is
never rewritten to consume `Advance.Host` (doing so would be a much larger, riskier rewrite of
HRM's own Identity/menu/tenant story that nothing in the plan's Phase 0-4 schedule calls for).

What this document can usefully hand the person scaffolding `Advance.Host` is which blocks of
HRM's `Program.cs` are the **source pattern to model, not lift verbatim** (each has HRM-specific
coupling — legacy `sc_user`, `HRMContext`, single-tenant `CompanyId` string — that a
multi-tenant shell must generalize, not copy):

| HRM.Program.cs lines | Pattern to generalize into Advance.Host | Why it can't be lifted verbatim |
|---|---|---|
| 39-64, 195-250 | Blazor Server + Identity core setup | Tied to `ApplicationDbContext`/`ApplicationUser`; Host needs its own Identity story per plan §3.2 ("login / ผู้ใช้ / รหัสผ่าน... host shell เดียวกัน") |
| 353-400 (minus line 381, superseded) | AD.CRUDManage route-scanning + per-role seeding | Tied to `sc_program_role`/`sc_program`; Host needs its own program-role table, but the *mechanism* (scan `@page` routes, longest-prefix match, memory-cached reads) is exactly what plan §3.2 says to reuse: "shell เดียวกัน: package ประกาศ route ให้ shell seed" |
| 359-374 | Menu/rights via `sc_menu`/`sc_role_menu`/claims | Host needs its own menu tables; a standalone Workflow/Payroll product has far fewer pages so this could be simpler, not a straight port |
| 656-688 | Rate limiting policies (login, forgot-password) | Directly reusable pattern, minimal HRM-specific coupling — closest thing to a verbatim lift in this table |
| none (new) | Tenant filtering (`TenantId`) | Doesn't exist in HRM's `Program.cs` at all today — HRM uses a bare `CompanyId` string per its own `CLAUDE.md` ("Company scoping is a string, not a numeric FK"); Host has to build this fresh, it isn't an extraction of anything |

If whoever scaffolds `Advance.Host` wants a single hook name to target for symmetry with the other
three, propose (unscaffolded, confirm against real code):

```csharp
builder.Services.AddAdvanceHost(builder.Configuration); // Identity + menu/rights + tenant + Excel import + Platform baseline
app.UseAdvanceHost();
```

— but note this call appears **only** in `Advance.Payroll.Web`'s and `Advance.Workflow.Web`'s own
`Program.cs` files (which don't exist yet either), never in HRM's.

---

## 4. Build order for the real cutover

Reasoning from actual `.csproj`/DI dependencies traced above, not an assumed default order:

1. **`Advance.SecurityCore` (whatever slice actually ships this wave) before anything else that
   needs `ProgramRoleService`/rate-limiting/security-headers patterns** — but per §3.3, most of
   SecurityCore's real payload (Identity) is explicitly deferred, so in practice this can land
   as a thin package (alert service, password-policy options, AD.CRUDManage, 3 middleware) without
   blocking the other three. Add its `Domain`/`Data` projects to `HRM.sln` before any `Web`/host
   piece, per the `ADVANCE-ARCHITECTURE.md` `X.Domain / X.Data / X.Web` layering the plan cites.
2. **`Advance.Workflow` before `Advance.Payroll`** — this is explicit in the plan itself (§6,
   decision row 6: "Workflow ก่อน — เล็กกว่า พึ่งพาน้อยกว่า และ Payroll ต้องใช้มัน (คำขอกองทุน)").
   Concretely: `Advance.Workflow.Contracts` (interfaces only, no dependencies) → `Advance.Workflow.Domain`
   (entities) → `Advance.Workflow.Data` (DbContext, depends on Domain) → `Advance.Workflow.Org`
   (depends on Contracts+Domain+Data) → `Advance.Workflow.Engine` (depends on all of the above) →
   `Advance.Workflow.Blazor` (RCL, depends on Engine+Contracts). HRM references `Contracts` +
   `Engine` + `Blazor`.
3. **`Advance.Payroll` after Workflow**, because Payroll's `wf_workflow`/`job_master` fund-change
   requests go through `IWorkflowEngine`/the workflow package (plan §2.1 table: "`wf_workflow`,
   `job_master` | คำขอกองทุน... | Advance.Workflow package"). Internally: `Advance.Payroll.Core`
   (pure calculators, zero dependencies — plan explicitly calls these "pure static... ไม่แตะฐานข้อมูล")
   → `Advance.Payroll.Domain` → `Advance.Payroll.Data` (depends on Domain) → `Advance.Payroll.Engine`
   (depends on Core+Domain+Data, and on `Advance.Workflow.Contracts` for the fund-change-request
   path) → `Advance.Payroll.Reports` (depends on Engine) → `Advance.Payroll.Blazor` (RCL, depends
   on Engine+Reports).
4. **`Advance.Host` last, and only for the two standalone-product solutions, not `HRM.sln`** — it
   has no consumers inside `HRM.sln` at all (§3.4), so adding it to `HRM.sln` only matters if the
   team wants HRM itself to eventually migrate onto the shell (out of scope per the plan's own
   Phase 0-4 table). `Advance.Payroll.Web`/`Advance.Workflow.Web` (Phase 3/4, separate repos per
   plan §6 decision row 2) reference `Advance.Host` plus their own product's Engine/Blazor/Data.

Net order for `HRM.sln` specifically: **SecurityCore(thin) → Workflow(6 projects, Contracts-first)
→ Payroll(7 projects, Core-first) → HRM.csproj's own `<ProjectReference>` additions from §2.3**.
`Advance.Host` never enters `HRM.sln` under this plan.

---

## 5. What could go wrong — top 3 concrete risks of this .sln/.csproj surgery

1. **MudBlazor version drift across 3+ `.csproj` files editing independently.** HRM.csproj pins
   `MudBlazor` `8.14.0` today. The moment `Advance.Workflow.Blazor` and `Advance.Payroll.Blazor`
   each declare their own `<PackageReference Include="MudBlazor" .../>` (required per §2.1 — RCLs
   need it at compile time, not just transitively), NuGet's version-unification rules mean the
   *highest* version anywhere in the graph wins for the final app, silently, unless every
   `.csproj` is kept in lockstep by hand or centralized via `Directory.Packages.props`
   (`ManagePackageVersionsCentrally`) — which doesn't exist in this repo today. Three agents
   scaffolding three sibling projects independently, on different days, each running
   `dotnet add package MudBlazor` with no pinned version, is exactly the scenario that produces
   a silent minor-version bump that breaks a component's markup in ways that only show at
   runtime (Blazor markup/API mismatches don't always fail the build). **Mitigate by pinning
   `8.14.0` explicitly in every new `.csproj` that references MudBlazor**, or by introducing
   central package management as a zeroth step before any new project is added.
2. **Stale `bin`/`obj` + the running `HRM.exe` lock (`MSB3027`/`MSB3021`) gets worse, not better,
   with 4x the project count.** This repo's own `CLAUDE.md` already documents that a running
   `dotnet run` locks HRM's own output and must be killed before a rebuild. Adding a
   `ProjectReference` graph across `HRM.csproj` → `Advance.Workflow.*` → `Advance.Payroll.*` means
   a stale `obj/project.assets.json` in *any* of the new projects (common right after `dotnet sln
   add`, before the first `dotnet restore`) manifests as a confusing "reference assembly not
   found" error in HRM's build that looks unrelated to the actual cause. The fix is always
   `dotnet restore` at the solution level immediately after each `dotnet sln add`, not per-project
   — easy to skip when adding projects one at a time across a long cutover session.
3. **Razor Class Library plumbing HRM currently gets for free as an executable `Sdk="Microsoft.NET.Sdk.Web"`
   project.** None of the classification in §2.1 mentions `<Project Sdk="Microsoft.NET.Sdk.Razor">`
   vs `Microsoft.NET.Sdk` vs `Microsoft.NET.Sdk.Web` deliberately — but it matters: `Advance.Workflow.Blazor`
   and `Advance.Payroll.Blazor` must be `Sdk="Microsoft.NET.Sdk.Razor"` (RCL) projects, and an RCL
   does **not** automatically get `<FrameworkReference Include="Microsoft.AspNetCore.App" />` the
   way `Microsoft.NET.Sdk.Web` does — component libraries usually still need it explicitly (or need
   `<PackageReference>`s that pull enough of the ASP.NET Core shared framework surface in) for
   things like `Microsoft.AspNetCore.Components.Web` types to resolve at compile time. Skipping
   this is a common first-build failure for a new RCL that "should just work" because it compiled
   fine as `.razor` files sitting inside the Web project before extraction. Confirm each new
   `Advance.*.Blazor` project's `<Project Sdk=...>` line and `<FrameworkReference>`/`<PackageReference>`
   set explicitly rather than assuming `dotnet new razorclasslib` produces something drop-in
   compatible with 28+41 pages worth of existing MudBlazor/QuickGrid/localization usage.
