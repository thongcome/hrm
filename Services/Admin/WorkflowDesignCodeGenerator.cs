using System.Text;

namespace HRM.Services.Admin;

public enum DesignFieldType { Text, Number, Decimal, Date, DateTime, Checkbox, Reference }

// Dropdown = MudSelect (small/fixed reference set, all rows loaded once).
// Autocomplete = MudAutocomplete with a live DB search (large/growing set).
// Mirrors the CRUD skill's §8 threshold rule — the designer, who knows the
// target table's real size, picks which one applies per field.
public enum ReferencePickerMode { Dropdown, Autocomplete }

public class DesignField
{
    public string ColumnName { get; set; } = "";
    public string LabelTh { get; set; } = "";
    public DesignFieldType Type { get; set; } = DesignFieldType.Text;
    public int Length { get; set; } = 200;
    public bool Required { get; set; }

    // Reference-only (Type == Reference). Populated by WorkflowDesign.razor
    // via reflection over HRMContext's DbSet<T> properties, so these always
    // name a real DbSet property + a real string property on a real model —
    // never free-typed by the designer.
    public string? RefDbSetProperty { get; set; }   // e.g. "Hremployee" -> context.Hremployee
    public string? RefEntityType { get; set; }       // CLR type name, e.g. "Hremployee"
    public string RefKeyProperty { get; set; } = "Id";
    public string? RefDisplayProperty { get; set; }  // label shown in the picker, e.g. "EmpName"
    public ReferencePickerMode RefPickerMode { get; set; } = ReferencePickerMode.Dropdown;
}

// A candidate FK target discovered by reflecting HRMContext's DbSet<T>
// properties (see WorkflowDesign.razor.LoadReferenceTargets). Only entities
// with a discoverable numeric key and at least one string property are
// offered — anything else can't be resolved to a Dictionary<long,string>
// label lookup or a long/long? soft-link column safely.
public record ReferenceTarget(string DbSetProperty, string EntityTypeName, string KeyProperty, List<string> StringProperties);

// Generates a Model class + HRMContext DbSet line + a 5-file basic CRUD
// page set (Index/Create/Edit/Details/Delete) for a table created via
// WorkflowDesign.razor's drag-and-drop designer. Mirrors the plain
// EditForm + IDbContextFactory + MudBlazor pattern used across this app
// (see Components/Pages/HrwOtPages/) and follows the two conventions
// codified in .claude/skills/CRUD/SKILL.md: a single AI-search box per
// list page (§2) and foreign keys resolved to pickers, never raw id
// columns (§8) — Reference fields become a MudSelect or MudAutocomplete
// depending on RefPickerMode, and list/detail views show the resolved
// label instead of the numeric id.
//
// Files are written to disk only (WorkflowDesign.razor does the actual
// File I/O) — they take effect after the next dotnet build / dev-server
// restart, same as any other scaffolded source, since the running
// assembly can't pick up brand new types without recompiling.
public static class WorkflowDesignCodeGenerator
{
    public static string GenerateEntityCode(string tableName, List<DesignField> fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine("namespace HRM.Models;");
        sb.AppendLine();
        sb.AppendLine($"[Table(\"{tableName}\")]");
        sb.AppendLine($"public class {tableName}");
        sb.AppendLine("{");
        sb.AppendLine("    [Key]");
        sb.AppendLine("    public long Id { get; set; }");
        sb.AppendLine();
        foreach (var f in fields)
        {
            AppendEntityProperty(sb, f);
            sb.AppendLine();
        }
        sb.AppendLine("    public DateTime CreatedDate { get; set; } = DateTime.Now;");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendEntityProperty(StringBuilder sb, DesignField f)
    {
        switch (f.Type)
        {
            case DesignFieldType.Text:
                sb.AppendLine($"    [StringLength({ClampLength(f.Length)})]");
                sb.AppendLine(f.Required
                    ? $"    public string {f.ColumnName} {{ get; set; }} = null!;"
                    : $"    public string? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.Number:
                sb.AppendLine(f.Required
                    ? $"    public int {f.ColumnName} {{ get; set; }}"
                    : $"    public int? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.Decimal:
                sb.AppendLine("    [Column(TypeName = \"decimal(18,2)\")]");
                sb.AppendLine(f.Required
                    ? $"    public decimal {f.ColumnName} {{ get; set; }}"
                    : $"    public decimal? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.Date:
                sb.AppendLine(f.Required
                    ? $"    public DateOnly {f.ColumnName} {{ get; set; }}"
                    : $"    public DateOnly? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.DateTime:
                sb.AppendLine(f.Required
                    ? $"    public DateTime {f.ColumnName} {{ get; set; }}"
                    : $"    public DateTime? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.Checkbox:
                sb.AppendLine(f.Required
                    ? $"    public bool {f.ColumnName} {{ get; set; }}"
                    : $"    public bool? {f.ColumnName} {{ get; set; }}");
                break;
            case DesignFieldType.Reference:
                sb.AppendLine($"    // soft-link -> {f.RefEntityType}.{f.RefKeyProperty} (label: {f.RefDisplayProperty})");
                if (f.Required)
                {
                    sb.AppendLine("    [Range(1, long.MaxValue, ErrorMessage = \"กรุณาเลือกข้อมูล\")]");
                    sb.AppendLine($"    public long {f.ColumnName} {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine($"    public long? {f.ColumnName} {{ get; set; }}");
                }
                break;
        }
    }

    private static int ClampLength(int length) => length is > 0 and <= 4000 ? length : 200;

    public static string GenerateDbSetLine(string tableName) =>
        $"    public virtual DbSet<{tableName}> {tableName}s {{ get; set; }}";

    public static string RouteSlug(string tableName) => tableName.ToLowerInvariant() + "s";

    private static bool IsReference(DesignField f) =>
        f.Type == DesignFieldType.Reference
        && !string.IsNullOrWhiteSpace(f.RefDbSetProperty)
        && !string.IsNullOrWhiteSpace(f.RefEntityType)
        && !string.IsNullOrWhiteSpace(f.RefDisplayProperty);

    // ---------------------------------------------------------------
    // Index page (MudTable + one search box + FK columns resolved to labels)
    // ---------------------------------------------------------------

    public static string GenerateIndexPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var textFields = fields.Where(f => f.Type == DesignFieldType.Text).ToList();
        var refFields = fields.Where(IsReference).ToList();

        var headerCols = string.Join("\n", fields.Select(f => $"            <MudTh>{f.LabelTh}</MudTh>"));
        var rowCols = string.Join("\n", fields.Select(RowCellMarkup));

        var searchBlock = textFields.Count == 0 ? "" : $"""

        <MudTextField T="string" Value="_searchTerm" Label="ค้นหา" Immediate="true"
            DebounceInterval="300" ValueChanged="OnSearchChanged"
            AdornmentIcon="@Icons.Material.Filled.Search" Adornment="Adornment.Start" Class="mb-3" />
""";

        var searchApply = textFields.Count == 0 ? "" :
            "\n        query = HRM.Services.Shared.EntitySearchHelper.ApplyTextSearch(query, _searchTerm, " +
            string.Join(", ", textFields.Select(f => $"nameof({tableName}.{f.ColumnName})")) + ");";

        var refLabelFields = string.Join("\n", refFields.Select(f =>
            $"    private Dictionary<long, string> _{f.ColumnName}Labels = new();"));

        var refLabelLoads = string.Join("\n", refFields.Select(f =>
            $"        _{f.ColumnName}Labels = (await context.{f.RefDbSetProperty}.Select(x => new {{ Key = x.{f.RefKeyProperty}, Value = x.{f.RefDisplayProperty} }}).ToListAsync())\n" +
            $"            .ToDictionary(x => Convert.ToInt64(x.Key), x => x.Value ?? \"\");"));

        return $$"""
@page "/{{route}}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using MudBlazor
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

<PageTitle>{{tableName}}</PageTitle>

<MudPaper Class="pa-4">
    <MudStack Row="true" AlignItems="AlignItems.Center" Justify="Justify.SpaceBetween" Class="mb-3">
        <MudText Typo="Typo.h5">{{tableName}}</MudText>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Create">เพิ่มรายการ</MudButton>
    </MudStack>
{{searchBlock}}
    <MudTable Items="_items" Hover="true" Dense="true" Loading="_loading">
        <HeaderContent>
            <MudTh>Id</MudTh>
{{headerCols}}
            <MudTh>จัดการ</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd>@context.Id</MudTd>
{{rowCols}}
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Filled.Visibility" Size="Size.Small" OnClick="@(() => Details(context.Id))" />
                <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" OnClick="@(() => Edit(context.Id))" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error" OnClick="@(() => Delete(context.Id))" />
            </MudTd>
        </RowTemplate>
    </MudTable>
</MudPaper>

@code {
    private List<{{tableName}}> _items = new();
    private bool _loading = true;
    private string _searchTerm = "";
{{refLabelFields}}

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        await using var context = await DbFactory.CreateDbContextAsync();
        var query = context.{{tableName}}s.AsQueryable();{{searchApply}}
        _items = await query.OrderByDescending(x => x.Id).ToListAsync();
{{refLabelLoads}}
        _loading = false;
    }

    private async Task OnSearchChanged(string term)
    {
        _searchTerm = term;
        await LoadAsync();
    }

    private void Create() => NavigationManager.NavigateTo("/{{route}}/create");
    private void Details(long id) => NavigationManager.NavigateTo($"/{{route}}/details/{id}");
    private void Edit(long id) => NavigationManager.NavigateTo($"/{{route}}/edit/{id}");
    private void Delete(long id) => NavigationManager.NavigateTo($"/{{route}}/delete/{id}");
}
""";
    }

    private static string RowCellMarkup(DesignField f)
    {
        if (IsReference(f))
        {
            return f.Required
                ? $"            <MudTd>@(_{f.ColumnName}Labels.TryGetValue(context.{f.ColumnName}, out var {f.ColumnName}Label) ? {f.ColumnName}Label : $\"#{{context.{f.ColumnName}}}\")</MudTd>"
                : $"            <MudTd>@(context.{f.ColumnName} is long {f.ColumnName}Id && _{f.ColumnName}Labels.TryGetValue({f.ColumnName}Id, out var {f.ColumnName}Label) ? {f.ColumnName}Label : \"-\")</MudTd>";
        }
        return f.Type switch
        {
            DesignFieldType.Checkbox => f.Required
                ? $"            <MudTd>@(context.{f.ColumnName} ? \"✓\" : \"\")</MudTd>"
                : $"            <MudTd>@(context.{f.ColumnName} == true ? \"✓\" : \"\")</MudTd>",
            DesignFieldType.Date => f.Required
                ? $"            <MudTd>@context.{f.ColumnName}.ToString(\"dd/MM/yyyy\")</MudTd>"
                : $"            <MudTd>@(context.{f.ColumnName}?.ToString(\"dd/MM/yyyy\") ?? \"-\")</MudTd>",
            DesignFieldType.DateTime => f.Required
                ? $"            <MudTd>@context.{f.ColumnName}.ToString(\"dd/MM/yyyy HH:mm\")</MudTd>"
                : $"            <MudTd>@(context.{f.ColumnName}?.ToString(\"dd/MM/yyyy HH:mm\") ?? \"-\")</MudTd>",
            _ => $"            <MudTd>@context.{f.ColumnName}</MudTd>",
        };
    }

    // ---------------------------------------------------------------
    // Create / Edit shared field markup (MudBlazor inputs, incl. FK pickers)
    // ---------------------------------------------------------------

    private static string FormFieldMarkup(DesignField f)
    {
        var label = $"{f.LabelTh} ({f.ColumnName})";

        if (IsReference(f))
        {
            return f.RefPickerMode == ReferencePickerMode.Dropdown
                ? DropdownFieldMarkup(f, label)
                : AutocompleteFieldMarkup(f, label);
        }

        return f.Type switch
        {
            DesignFieldType.Checkbox =>
                $"    <MudCheckBox T=\"bool\" Value=\"item.{f.ColumnName} == true\" ValueChanged=\"@(v => item.{f.ColumnName} = v)\" Label=\"{label}\" Class=\"mb-3\" />",
            DesignFieldType.Date =>
                f.Required
                    ? $"    <MudDatePicker Label=\"{label}\" Editable=\"true\" DateFormat=\"dd/MM/yyyy\"\n" +
                      $"        Date=\"@item.{f.ColumnName}.ToDateTime(TimeOnly.MinValue)\"\n" +
                      $"        DateChanged=\"@(d => item.{f.ColumnName} = d.HasValue ? DateOnly.FromDateTime(d.Value) : item.{f.ColumnName})\" Class=\"mb-3\" />"
                    : $"    <MudDatePicker Label=\"{label}\" Editable=\"true\" DateFormat=\"dd/MM/yyyy\"\n" +
                      $"        Date=\"@(item.{f.ColumnName}.HasValue ? item.{f.ColumnName}!.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null)\"\n" +
                      $"        DateChanged=\"@(d => item.{f.ColumnName} = d.HasValue ? DateOnly.FromDateTime(d.Value) : null)\" Class=\"mb-3\" />",
            DesignFieldType.DateTime =>
                f.Required
                    ? $"    <MudDatePicker Label=\"{label}\" Editable=\"true\" DateFormat=\"dd/MM/yyyy\"\n" +
                      $"        Date=\"@item.{f.ColumnName}\" DateChanged=\"@(d => item.{f.ColumnName} = d ?? item.{f.ColumnName})\" Class=\"mb-3\" />"
                    : $"    <MudDatePicker Label=\"{label}\" Editable=\"true\" DateFormat=\"dd/MM/yyyy\" @bind-Date=\"item.{f.ColumnName}\" Class=\"mb-3\" />",
            DesignFieldType.Number =>
                $"    <MudNumericField T=\"{(f.Required ? "int" : "int?")}\" @bind-Value=\"item.{f.ColumnName}\" Label=\"{label}\" Class=\"mb-3\" />",
            DesignFieldType.Decimal =>
                $"    <MudNumericField T=\"{(f.Required ? "decimal" : "decimal?")}\" @bind-Value=\"item.{f.ColumnName}\" Label=\"{label}\" Format=\"N2\" Class=\"mb-3\" />",
            _ =>
                $"    <MudTextField T=\"string\" @bind-Value=\"item.{f.ColumnName}\" Label=\"{label}\" MaxLength=\"{ClampLength(f.Length)}\" Required=\"{(f.Required ? "true" : "false")}\" Class=\"mb-3\" />",
        };
    }

    private static string DropdownFieldMarkup(DesignField f, string label)
    {
        if (f.Required)
        {
            return $$"""
    <MudSelect T="long" Value="item.{{f.ColumnName}}" ValueChanged="@(v => item.{{f.ColumnName}} = v)" Label="{{label}}" Required="true" For="@(() => item.{{f.ColumnName}})" Class="mb-3">
        @foreach (var opt in _{{f.ColumnName}}Options)
        {
            <MudSelectItem T="long" Value="opt.Id">@opt.Label</MudSelectItem>
        }
    </MudSelect>
""";
        }
        return $$"""
    <MudSelect T="long?" Value="item.{{f.ColumnName}}" ValueChanged="@(v => item.{{f.ColumnName}} = v)" Label="{{label}}" Clearable="true" Class="mb-3">
        @foreach (var opt in _{{f.ColumnName}}Options)
        {
            <MudSelectItem T="long?" Value="@((long?)opt.Id)">@opt.Label</MudSelectItem>
        }
    </MudSelect>
""";
    }

    private static string AutocompleteFieldMarkup(DesignField f, string label)
    {
        var assign = f.Required
            ? $"v?.{f.RefKeyProperty} ?? 0"
            : $"v?.{f.RefKeyProperty}";
        var requiredAttrs = f.Required ? " Required=\"true\" For=\"@(() => item." + f.ColumnName + ")\"" : " Clearable=\"true\"";
        return $"    <MudAutocomplete T=\"{f.RefEntityType}\" Value=\"_{f.ColumnName}Selected\" " +
               $"ValueChanged=\"@(v => {{ _{f.ColumnName}Selected = v; item.{f.ColumnName} = {assign}; }})\"\n" +
               $"        SearchFunc=\"Search{f.ColumnName}Async\" ToStringFunc=\"@(x => x?.{f.RefDisplayProperty} ?? \"\")\" ResetValueOnEmptyText=\"true\"\n" +
               $"        Label=\"{label}\"{requiredAttrs} Class=\"mb-3\" />";
    }

    private static string RefStateFields(DesignField f) =>
        f.RefPickerMode == ReferencePickerMode.Dropdown
            ? $"    private List<(long Id, string Label)> _{f.ColumnName}Options = new();"
            : $"    private {f.RefEntityType}? _{f.ColumnName}Selected;";

    private static string RefSearchMethod(DesignField f) => $$"""

    private async Task<IEnumerable<{{f.RefEntityType}}>> Search{{f.ColumnName}}Async(string search, CancellationToken ct)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var query = context.{{f.RefDbSetProperty}}.AsQueryable();
        query = HRM.Services.Shared.EntitySearchHelper.ApplyTextSearch(query, search, nameof({{f.RefEntityType}}.{{f.RefDisplayProperty}}));
        return await query.OrderBy(x => x.{{f.RefDisplayProperty}}).Take(20).ToListAsync();
    }
""";

    // Loaded inside the same already-open `context` block that loads/saves
    // the page's other data — for Dropdown mode this preloads every option
    // once; Autocomplete mode needs no preload (it searches on demand).
    private static string RefDropdownLoadCode(DesignField f) =>
        $"        var {f.ColumnName}Raw = await context.{f.RefDbSetProperty}.Select(x => new {{ x.{f.RefKeyProperty}, x.{f.RefDisplayProperty} }}).ToListAsync();\n" +
        $"        _{f.ColumnName}Options = {f.ColumnName}Raw.Select(x => (Convert.ToInt64(x.{f.RefKeyProperty}), x.{f.RefDisplayProperty} ?? \"\")).OrderBy(x => x.Item2).ToList();";

    // Edit page only: resolve the entity currently referenced by item.{col}
    // so MudAutocomplete shows the existing selection instead of blank.
    private static string RefAutocompleteEditLoadCode(DesignField f)
    {
        if (f.Required)
        {
            return $"        if (item.{f.ColumnName} > 0)\n" +
                   $"        {{\n" +
                   $"            _{f.ColumnName}Selected = await context.{f.RefDbSetProperty}.FirstOrDefaultAsync(x => x.{f.RefKeyProperty} == item.{f.ColumnName});\n" +
                   $"        }}";
        }
        return $"        if (item.{f.ColumnName} is long {f.ColumnName}Val)\n" +
               $"        {{\n" +
               $"            _{f.ColumnName}Selected = await context.{f.RefDbSetProperty}.FirstOrDefaultAsync(x => x.{f.RefKeyProperty} == {f.ColumnName}Val);\n" +
               $"        }}";
    }

    public static string GenerateCreatePage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var inputs = string.Join("\n", fields.Select(FormFieldMarkup));
        var refFields = fields.Where(IsReference).ToList();

        var refStateFields = string.Join("\n", refFields.Select(RefStateFields));
        var refSearchMethods = string.Join("", refFields.Where(f => f.RefPickerMode == ReferencePickerMode.Autocomplete).Select(RefSearchMethod));
        var refDropdownLoads = string.Join("\n", refFields.Where(f => f.RefPickerMode == ReferencePickerMode.Dropdown).Select(RefDropdownLoadCode));
        var loadOptionsBlock = refDropdownLoads.Length == 0 ? "" : $$"""


    private async Task LoadOptionsAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
{{refDropdownLoads}}
    }
""";
        var loadOptionsCall = refDropdownLoads.Length == 0 ? "" : "\n        await LoadOptionsAsync();";

        return $$"""
@page "/{{route}}/create"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using MudBlazor
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

<MudPaper Class="pa-4">
    <MudText Typo="Typo.h5" Class="mb-3">Create {{tableName}}</MudText>

    <EditForm Model="item" OnValidSubmit="SaveAsync">
        <DataAnnotationsValidator />
        <ValidationSummary />

{{inputs}}

        <MudButton Color="Color.Primary" Variant="Variant.Filled" ButtonType="ButtonType.Submit">บันทึก</MudButton>
        <MudButton Variant="Variant.Text" Href="/{{route}}" Class="ml-2">ยกเลิก</MudButton>
    </EditForm>
</MudPaper>

@code {
    private {{tableName}} item = new();
{{refStateFields}}

    protected override async Task OnInitializedAsync()
    {{{loadOptionsCall}}
    }
{{loadOptionsBlock}}
    private async Task SaveAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.{{tableName}}s.Add(item);
        await context.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
{{refSearchMethods}}
}
""";
    }

    public static string GenerateEditPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var inputs = string.Join("\n", fields.Select(FormFieldMarkup));
        var refFields = fields.Where(IsReference).ToList();

        var refStateFields = string.Join("\n", refFields.Select(RefStateFields));
        var refSearchMethods = string.Join("", refFields.Where(f => f.RefPickerMode == ReferencePickerMode.Autocomplete).Select(RefSearchMethod));
        var refDropdownLoads = string.Join("\n", refFields.Where(f => f.RefPickerMode == ReferencePickerMode.Dropdown).Select(RefDropdownLoadCode));
        var refAutocompleteLoads = string.Join("\n", refFields.Where(f => f.RefPickerMode == ReferencePickerMode.Autocomplete).Select(RefAutocompleteEditLoadCode));
        var refLoadBlock = (refDropdownLoads + refAutocompleteLoads).Length == 0 ? "" : "\n" + string.Join("\n", new[] { refDropdownLoads, refAutocompleteLoads }.Where(s => s.Length > 0));

        return $$"""
@page "/{{route}}/edit/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using MudBlazor
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

@if (item is null)
{
    <MudText>กำลังโหลด...</MudText>
}
else
{
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h5" Class="mb-3">Edit {{tableName}} #@item.Id</MudText>
        <EditForm Model="item" OnValidSubmit="SaveAsync">
            <DataAnnotationsValidator />
            <ValidationSummary />

{{inputs}}

            <MudButton Color="Color.Primary" Variant="Variant.Filled" ButtonType="ButtonType.Submit">บันทึก</MudButton>
            <MudButton Variant="Variant.Text" Href="/{{route}}" Class="ml-2">ยกเลิก</MudButton>
        </EditForm>
    </MudPaper>
}

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;
{{refStateFields}}

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        item = await context.{{tableName}}s.FindAsync(id);
        if (item is null)
        {
            NavigationManager.NavigateTo("/{{route}}");
            return;
        }
{{refLoadBlock}}
    }

    private async Task SaveAsync()
    {
        if (item is null) return;
        await using var context = await DbFactory.CreateDbContextAsync();
        context.{{tableName}}s.Update(item);
        await context.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
{{refSearchMethods}}
}
""";
    }

    public static string GenerateDetailsPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var refFields = fields.Where(IsReference).ToList();

        var rows = string.Join("\n", fields.Select(f =>
            IsReference(f)
                ? $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@(_{f.ColumnName}Label ?? \"-\")</b></MudListItem>"
                : DetailRowMarkup(f)));

        var refLabelFields = string.Join("\n", refFields.Select(f => $"    private string? _{f.ColumnName}Label;"));
        var refLabelLoads = string.Join("\n", refFields.Select(f =>
            f.Required
                ? $"        _{f.ColumnName}Label = await context.{f.RefDbSetProperty}.Where(x => x.{f.RefKeyProperty} == item.{f.ColumnName}).Select(x => x.{f.RefDisplayProperty}).FirstOrDefaultAsync();"
                : $"        if (item.{f.ColumnName} is long {f.ColumnName}Val)\n" +
                  $"        {{\n" +
                  $"            _{f.ColumnName}Label = await context.{f.RefDbSetProperty}.Where(x => x.{f.RefKeyProperty} == {f.ColumnName}Val).Select(x => x.{f.RefDisplayProperty}).FirstOrDefaultAsync();\n" +
                  $"        }}"));
        var refLoadBlock = refLabelLoads.Length == 0 ? "" : "\n" + refLabelLoads;

        return $$"""
@page "/{{route}}/details/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using MudBlazor
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory

@if (item is null)
{
    <MudText>ไม่พบข้อมูล</MudText>
}
else
{
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h5" Class="mb-3">{{tableName}} #@item.Id</MudText>
        <MudList T="string" Dense="true">
{{rows}}
            <MudListItem>สร้างเมื่อ: <b>@item.CreatedDate</b></MudListItem>
        </MudList>
        <MudStack Row="true" Spacing="2" Class="mt-3">
            <MudButton Variant="Variant.Text" Href="/{{route}}">กลับ</MudButton>
            <MudButton Color="Color.Primary" Variant="Variant.Filled" Href="/{{route}}/edit/@item.Id">แก้ไข</MudButton>
        </MudStack>
    </MudPaper>
}

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;
{{refLabelFields}}

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        item = await context.{{tableName}}s.FindAsync(id);
        if (item is null) return;
{{refLoadBlock}}
    }
}
""";
    }

    private static string DetailRowMarkup(DesignField f) => f.Type switch
    {
        DesignFieldType.Checkbox => f.Required
            ? $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@(item.{f.ColumnName} ? \"ใช่\" : \"ไม่ใช่\")</b></MudListItem>"
            : $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@(item.{f.ColumnName} == true ? \"ใช่\" : \"ไม่ใช่\")</b></MudListItem>",
        DesignFieldType.Date => f.Required
            ? $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@item.{f.ColumnName}.ToString(\"dd/MM/yyyy\")</b></MudListItem>"
            : $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@(item.{f.ColumnName}?.ToString(\"dd/MM/yyyy\") ?? \"-\")</b></MudListItem>",
        DesignFieldType.DateTime => f.Required
            ? $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@item.{f.ColumnName}.ToString(\"dd/MM/yyyy HH:mm\")</b></MudListItem>"
            : $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@(item.{f.ColumnName}?.ToString(\"dd/MM/yyyy HH:mm\") ?? \"-\")</b></MudListItem>",
        _ => $"        <MudListItem>{f.LabelTh} ({f.ColumnName}): <b>@item.{f.ColumnName}</b></MudListItem>",
    };

    public static string GenerateDeletePage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);

        return $$"""
@page "/{{route}}/delete/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using MudBlazor
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

@if (item is null)
{
    <MudText>ไม่พบข้อมูล</MudText>
}
else
{
    <MudPaper Class="pa-4">
        <MudText Typo="Typo.h5" Class="mb-3">Delete {{tableName}}</MudText>
        <MudAlert Severity="Severity.Warning" Class="mb-3">ยืนยันการลบรายการ #@item.Id ใช่หรือไม่?</MudAlert>
        <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="DeleteConfirmedAsync">ลบ</MudButton>
        <MudButton Variant="Variant.Text" Href="/{{route}}" Class="ml-2">ยกเลิก</MudButton>
    </MudPaper>
}

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        item = await context.{{tableName}}s.FindAsync(id);
        if (item is null) NavigationManager.NavigateTo("/{{route}}");
    }

    private async Task DeleteConfirmedAsync()
    {
        if (item is null) return;
        await using var context = await DbFactory.CreateDbContextAsync();
        context.{{tableName}}s.Remove(item);
        await context.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
}
""";
    }
}
