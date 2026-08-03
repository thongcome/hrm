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
