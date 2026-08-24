---
name: CRUD
description: Conventions for building a new CRUD page (list/search/create/edit) in the HRM Blazor project. Use this whenever adding a new admin/management page for a database table in this codebase — creating, editing, or listing any entity, building a "จัดการ..." page, or scaffolding a new Pay_*/Att_*/Lve_*/Com_*/sc_*/wf_* admin screen — even if the user doesn't say "CRUD" explicitly. Also consult it before designing a brand-new table, since this codebase often already has a legacy table for the need.
---

# CRUD pages in HRM

These are standing conventions agreed with the user after trying (and reverting) a generic shared-component approach. The user's own words on why: the direct pattern is "ตรงตัวและใช้งานง่ายมาก" (direct and very easy to work with) — the generic component added structure nobody asked for. Treat that as the reason behind every rule below, not just the rule itself.

## 1. Write the page directly — no shared CRUD shell component

A new CRUD page is a plain Razor page: `EditForm`/`@bind-Value` bound straight to the entity, `IDbContextFactory<HRMContext>` injected directly into `@code`. No generic wrapper component sits between the page and the data.

Follow the shape of `Components/Pages/HrwOtPages/Index.razor` + `Create.razor`, or `Components/Pages/wf_workflowPages/` — copy their structure for a new table rather than inventing a new pattern.

`Components/Shared/CrudScaffold.razor` exists in this codebase — an earlier attempt at a generic, reusable shell (search + PDPA logging + read-only lock, all built in). Don't reach for it on new pages. It's kept alive only for the two pages that were deliberately built on it already (`Components/Pages/Wf/WfOrgTypeAdmin.razor`, `WfEmployeeAdmin.razor`) — leave those as-is, but every new page goes direct-pattern.

## 2. One search box per list page, searching everything at once

Every list page gets exactly one text box that LIKE-searches across all the relevant string columns simultaneously — not a filter per column. Users think "find the row with X in it somewhere," not "which field is X in."

Use `Services/Shared/EntitySearchHelper.cs`:

```csharp
query = EntitySearchHelper.ApplyTextSearch(query, searchTerm, nameof(Entity.Code), nameof(Entity.Name), nameof(Entity.NameEn));
```

This helper is standalone — it doesn't depend on `CrudScaffold.razor` — so it works fine in a direct-pattern page. Bind it to a debounced `MudTextField` so typing doesn't refire the query on every keystroke:

```razor
<MudTextField T="string" Value="_searchTerm" Label="ค้นหา" Immediate="true"
    DebounceInterval="300" ValueChanged="OnSearchChanged"
    AdornmentIcon="@Icons.Material.Filled.Search" Adornment="Adornment.Start" />
```

Note: bind with `Value`/`ValueChanged`, not `@bind-Value`, when you also need an explicit `ValueChanged` handler — combining `@bind-Value` with an explicit `ValueChanged` on the same component throws a Razor compile error (RZ10010) because `@bind-Value` already generates one.

**Don't auto-load the full list on page entry for a table that can grow large.** The debounced live-search above is fine for a small, bounded lookup table (a handful to a few dozen rows — e.g. a status/category list) where loading everything up front costs nothing. For anything that can realistically grow into the hundreds or thousands of rows (most real business entities — organizations, employees, transactions), the page must start empty with a prompt ("กรอกคำค้น... แล้วกด \"ค้นหา\" เพื่อแสดงรายการ") and only query once the user explicitly searches — a "ค้นหา" button (plus Enter-to-search on the text field is fine as a convenience) rather than a query firing on `OnInitializedAsync`. See `Components/Pages/Org/OrganizationAdmin.razor` for the reference shape: `_hasSearched` gates whether the table or the prompt renders, and `SearchAsync()` is only ever called from the button/Enter-key handler, never from `OnInitializedAsync`.

## 3. PDPA badge + access log on pages showing personal data

If a page shows anything PDPA-sensitive — national ID, salary, bank account, address, health info, and the like — do two things:

- Show a visible "PDPA" chip near the page title, so nobody is looking at protected personal data without knowing it.
- Call `IAuditLogger.LogAccessAsync(entityType, recordId, isSensitive: true, note: "view")` when a record is opened.

`Model/HRMContext.Audit.cs` already logs every Create/Update/Delete across the whole system automatically via a `SaveChangesAsync` override — you don't need to call anything for those. The only gap it can't cover is *viewing* a record without changing it, since that never touches `SaveChanges`. That's what the explicit `LogAccessAsync` call on open/edit is for.

This is in service of a standing requirement: every sensitive access or change needs actor + IP + timestamp, kept for at least 90 days, never auto-purged before that (Thai พ.ร.บ.คอมพิวเตอร์ compliance). See `Components/Pages/Wf/WfEmployeeAdmin.razor` for a page that already does this.

Reference for the badge:

```razor
<MudChip T="string" Color="Color.Warning" Size="Size.Small">PDPA</MudChip>
```

## 4. Ship the plain CRUD first, layer logic on afterward

Get list → search → create → edit → soft-delete working and verified before adding any non-trivial business logic (calculations, workflow, approval routing, whatever the table is really *for*). Two separate steps, not one. The first pass proves the data model and the page shape are right before anything harder gets built on top of them — cheaper to fix a wrong field then than after logic depends on it.

## 5. Before modeling a new table, check whether one already exists

This codebase was scaffolded from a legacy JSP-era HR system, and a large number of its tables (20+) were carried over into the EF model but never wired to any UI. Before writing a new `Model/Whatever.cs`, search the existing `Model/` folder and the legacy schema dump (if referenced elsewhere in the project) for a table that already covers the need — reusing a dormant table beats designing a fresh one, and keeps the data shape consistent with whatever else in the system already reads that legacy table's siblings.

## 6. Company scoping and soft delete, when relevant

- If the table is company-specific, scope it with a **string** `CompanyId`/`companyid` field matching `Hremployee.companyid` — not a numeric FK to some other company table. This matches the convention used across `Pay_*`/`Lve_*`/`Att_*` tables and the `payroll_company` claim already used for filtering everywhere else.
- Prefer soft delete: an `IsActive`/`isactive` boolean flipped off, not a hard `DELETE`. Keeps history intact and matches what every other admin page in this codebase does.

## 7. Two page shapes — pick one deliberately

Every CRUD page in this codebase is one of two shapes. Pick the one that matches the table before writing anything; don't default to whichever is easier to type.

**Shape A — Full Edit Form.** One page, one form, every editable field on it at once, grouped under `Typo.h6` section headings when the table is wide. This is the default shape — use it unless the table clearly benefits from Shape B.

- Reference for section grouping: `Components/Pages/Pay/PayslipSettingsAdmin.razor` — five distinct `<MudText Typo="Typo.h6" Class="mb-2">` headings ("ข้อมูลบริษัท...", "ปีบัญชี (Fiscal Year)", "ทดลองงาน (Probation)", etc.) each followed by the fields that belong to that group. Copy this structure directly for any table wide enough to need it (roughly 8+ fields). For a narrow table (under ~8 fields), skip the headings and just list the fields — don't invent groups that don't earn their keep.
- `Components/Pages/Wf/WfWorkflowAdmin.razor` covers all 33 `wf_workflow` fields on one page but does **not** use section headings (just bare `MudGrid` blocks) — it's a working example of "everything on one form" but not of the grouped-section layout. Don't cite it as a grouped-section reference.

**Shape B — Master-Detail.** Two panes (or a list + a drill-in detail page): the master list on one side, and clicking a row opens its full detail/edit view — either inline (`Components/Pages/Pay/PayrollEmployeeDetail.razor`-style, a dedicated `/xxx/{id}` route) or as a side panel. Reach for this when a record has enough weight or enough related child data that cramming it into one flat form would bury the primary fields (e.g. an employee with core fields + several child collections). As of this writing there is no page in this codebase built exactly as a two-pane master-detail CRUD shell — `PayrollEmployeeDetail.razor` is the closest real precedent (list → dedicated detail route) but is itself a hand-built page tied to payroll, not a generic pattern. The first page built this way becomes the reference for the next one; don't invent a second, different master-detail layout once one exists.

Decision rule: start with Shape A. Move to Shape B only when the table has real child collections/tabs to manage, or the field count makes a single form unreadable even with section headings.

## 8. Foreign keys are pickers, never raw ID fields

Never put a raw numeric ID in a text box or unlabeled `MudNumericField` for a foreign key. Resolve it to a picker that shows the human-readable label:

- **Small/fixed reference set** (a handful to a few dozen rows, e.g. status, category, type lookup tables) → `MudSelect<T>` bound to the full in-memory list loaded once in `OnInitializedAsync`.
- **Large/growing reference set** (e.g. picking an employee out of the whole company) → `MudAutocomplete<T>` with a `SearchFunc` that queries the database live rather than loading everything. Reference: `Components/Pages/Pay/WithholdingCertificateGenerate.razor` — `MudAutocomplete T="Hremployee"` bound via `SearchFunc="SearchEmployeesAsync"`, where `SearchEmployeesAsync` runs an `EntitySearchHelper`-backed query scoped to the current company. The same pattern is used in `SalaryCertificateGenerate.razor` and `Por1Generate.razor`. Copy this shape rather than writing a new autocomplete query per page.

Never display a bare `EmployeeId`/`XxxId` column in a list view either — join/project to the display name in the query, or resolve it via a dictionary built once per page load.

**Don't add a shortcut button/link on the referencing page to manage the reference data.** If a picker's source table (`Currency`, a status lookup, whatever) already has its own CRUD page, that page is where it gets managed — don't put a "จัดการสกุลเงิน" / "+ เพิ่ม..." button or nav link on the page that merely *references* it. It's redundant (the reference data's own CRUD page is already reachable from the main nav) and it clutters a page that should stay focused on its own entity. If the picker list is empty, an inline hint is fine ("ยังไม่มีข้อมูล — ไปที่หน้า [X] เพื่อเพิ่มก่อน") but it should be text, not a button/link that turns the page into a hub for managing other tables.

## 9. Landing state is always List or View — never a Create/Edit form

The first thing a user sees when they open a CRUD page's main route must be the **list** (or, for a detail route, a **read-only view**). Never a Create/Edit form. The user's own words on why: "เข้าไปถึง ให้แก้ ให้เพิ่มเลย คนจะงง บางทีเข้าไปดู แล้วทำไม ไม่เจอข้อมูล ไปเจอหน้าแก้" — landing on an edit form instead of data reads as "where did my data go," even when the data is right there, one scroll away.

This rules out the earlier version of Shape A where the create/edit form sat *above* the list on the same page (`OrganizationAdmin.razor` did exactly this before it was fixed — the "เพิ่มข้อมูลสังกัด" form was the first thing rendered, with the actual list of organizations below a divider, out of the initial viewport). If Shape A's form and list share one page, the **list comes first**; a form only appears after the user takes an explicit action (clicking "เพิ่ม" or a row's "แก้ไข"), and should be dismissible back to the list, not something they land in.

Prefer splitting Create and Edit into their own routes entirely (mirrors Shape B) once the form has enough fields or business logic that it would otherwise dominate the list page — see `Components/Pages/Org/OrgUnitList.razor` + `OrgUnitCreate.razor` + `OrgUnitEdit.razor` for the reference shape: a pure list page (search, table, a "เพิ่ม..." button that navigates to a dedicated create route), a dedicated create route, and a dedicated edit route — none of which is what a user lands on by default.

**Workflow/approval context belongs on the View or Edit/Create page, not on the list.** If saving a record requires approval (e.g. "การย้ายสังกัดต้องผ่านการอนุมัติ — ระบุวันที่มีผล"), that alert and its accompanying fields (effective date, request note) belong inside the Create/Edit form where they're relevant to the action being taken — never bolted onto the list page a user sees by default.
