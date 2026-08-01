using System.Text;

namespace HRM.Services.Admin;

public enum DesignFieldType { Text, Number, Decimal, Date, DateTime, Checkbox }

public class DesignField
{
    public string ColumnName { get; set; } = "";
    public string LabelTh { get; set; } = "";
    public DesignFieldType Type { get; set; } = DesignFieldType.Text;
    public int Length { get; set; } = 200;
    public bool Required { get; set; }
}

// Generates a Model class + HRMContext DbSet line + a 5-file basic CRUD
// page set (Index/Create/Edit/Details/Delete) for a table created via
// WorkflowDesign.razor's drag-and-drop designer. Deliberately mirrors the
// plain EditForm + IDbContextFactory pattern already used across this app
// (see wf_workflowPages/wf_loaPages) rather than inventing a new style —
// that's the explicitly preferred CRUD convention for this project.
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
        }
    }

    private static int ClampLength(int length) => length is > 0 and <= 4000 ? length : 200;

    public static string GenerateDbSetLine(string tableName) =>
        $"    public virtual DbSet<{tableName}> {tableName}s {{ get; set; }}";

    public static string RouteSlug(string tableName) => tableName.ToLowerInvariant() + "s";

    private static string InputMarkup(DesignField f)
    {
        var id = f.ColumnName.ToLowerInvariant();
        return f.Type switch
        {
            DesignFieldType.Checkbox =>
                $"        <div class=\"mb-3 form-check\">\n" +
                $"            <input type=\"checkbox\" id=\"{id}\" class=\"form-check-input\" @bind=\"item.{f.ColumnName}\" />\n" +
                $"            <label for=\"{id}\" class=\"form-check-label\">{f.LabelTh} ({f.ColumnName})</label>\n" +
                $"        </div>",
            DesignFieldType.Date or DesignFieldType.DateTime =>
                $"        <div class=\"mb-3\">\n" +
                $"            <label for=\"{id}\" class=\"form-label\">{f.LabelTh} ({f.ColumnName}):</label>\n" +
                $"            <InputDate id=\"{id}\" @bind-Value=\"item.{f.ColumnName}\" class=\"form-control\" />\n" +
                $"        </div>",
            DesignFieldType.Number or DesignFieldType.Decimal =>
                $"        <div class=\"mb-3\">\n" +
                $"            <label for=\"{id}\" class=\"form-label\">{f.LabelTh} ({f.ColumnName}):</label>\n" +
                $"            <InputNumber id=\"{id}\" @bind-Value=\"item.{f.ColumnName}\" class=\"form-control\" />\n" +
                $"        </div>",
            _ =>
                $"        <div class=\"mb-3\">\n" +
                $"            <label for=\"{id}\" class=\"form-label\">{f.LabelTh} ({f.ColumnName}):</label>\n" +
                $"            <InputText id=\"{id}\" @bind-Value=\"item.{f.ColumnName}\" class=\"form-control\" />\n" +
                $"        </div>",
        };
    }

    public static string GenerateIndexPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var headerCols = string.Join("\n", fields.Select(f => $"                <th>{f.LabelTh}</th>"));
        var rowCols = string.Join("\n", fields.Select(f => $"                    <td>@item.{f.ColumnName}</td>"));

        return $$"""
@page "/{{route}}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

<h3>{{tableName}}</h3>

<p>
    <button class="btn btn-primary" @onclick="Create">Create New</button>
</p>

@if (items is null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table table-striped">
        <thead>
            <tr>
                <th>Id</th>
{{headerCols}}
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in items)
            {
                <tr>
                    <td>@item.Id</td>
{{rowCols}}
                    <td>
                        <a class="btn btn-sm btn-info me-1" href="/{{route}}/details/@item.Id">Details</a>
                        <a class="btn btn-sm btn-secondary me-1" href="/{{route}}/edit/@item.Id">Edit</a>
                        <a class="btn btn-sm btn-danger" href="/{{route}}/delete/@item.Id">Delete</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<{{tableName}}>? items;

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        items = await db.{{tableName}}s.OrderBy(x => x.Id).ToListAsync();
    }

    private void Create() => NavigationManager.NavigateTo("/{{route}}/create");
}
""";
    }

    public static string GenerateCreatePage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var inputs = string.Join("\n", fields.Select(InputMarkup));

        return $$"""
@page "/{{route}}/create"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

<h3>Create {{tableName}}</h3>

<EditForm Model="item" OnValidSubmit="SaveAsync">
    <DataAnnotationsValidator />
    <ValidationSummary />

{{inputs}}

    <button class="btn btn-primary">Save</button>
    <a class="btn btn-secondary ms-2" href="/{{route}}">Cancel</a>
</EditForm>

@code {
    private {{tableName}} item = new();

    private async Task SaveAsync()
    {
        using var db = DbFactory.CreateDbContext();
        db.{{tableName}}s.Add(item);
        await db.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
}
""";
    }

    public static string GenerateEditPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var inputs = string.Join("\n", fields.Select(InputMarkup));

        return $$"""
@page "/{{route}}/edit/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

@if (item is null)
{
    <p><em>Loading...</em></p>
}
else
{
    <h3>Edit {{tableName}} #@item.Id</h3>
    <EditForm Model="item" OnValidSubmit="SaveAsync">
        <DataAnnotationsValidator />
        <ValidationSummary />

{{inputs}}

        <button class="btn btn-primary">Save</button>
        <a class="btn btn-secondary ms-2" href="/{{route}}">Cancel</a>
    </EditForm>
}

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        item = await db.{{tableName}}s.FindAsync(id);
        if (item is null)
        {
            NavigationManager.NavigateTo("/{{route}}");
        }
    }

    private async Task SaveAsync()
    {
        if (item is null) return;
        using var db = DbFactory.CreateDbContext();
        db.{{tableName}}s.Update(item);
        await db.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
}
""";
    }

    public static string GenerateDetailsPage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);
        var rows = string.Join("\n", fields.Select(f =>
            $"        <dt class=\"col-sm-3\">{f.LabelTh} ({f.ColumnName})</dt>\n        <dd class=\"col-sm-9\">@item.{f.ColumnName}</dd>"));

        return $$"""
@page "/{{route}}/details/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory

@if (item is null)
{
    <p><em>Not found</em></p>
}
else
{
    <h3>{{tableName}} #@item.Id</h3>
    <dl class="row">
{{rows}}
        <dt class="col-sm-3">สร้างเมื่อ (CreatedDate)</dt>
        <dd class="col-sm-9">@item.CreatedDate</dd>
    </dl>

    <a class="btn btn-secondary" href="/{{route}}">Back</a>
    <a class="btn btn-info" href="/{{route}}/edit/@item.Id">Edit</a>
}

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        item = await db.{{tableName}}s.FindAsync(id);
    }
}
""";
    }

    public static string GenerateDeletePage(string tableName, List<DesignField> fields)
    {
        var route = RouteSlug(tableName);

        return $$"""
@page "/{{route}}/delete/{id:long}"
@using HRM.Models
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@attribute [Authorize(Policy = "Menu:SYS_ADMIN")]
@inject IDbContextFactory<HRMContext> DbFactory
@inject NavigationManager NavigationManager

@code {
    [Parameter]
    public long id { get; set; }

    private {{tableName}}? item;

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        item = await db.{{tableName}}s.FindAsync(id);
        if (item is null) NavigationManager.NavigateTo("/{{route}}");
    }

    private async Task DeleteConfirmedAsync()
    {
        if (item is null) return;
        using var db = DbFactory.CreateDbContext();
        db.{{tableName}}s.Remove(item);
        await db.SaveChangesAsync();
        NavigationManager.NavigateTo("/{{route}}");
    }
}

@if (item is null)
{
    <p><em>Not found</em></p>
}
else
{
    <h3>Delete {{tableName}}</h3>
    <p>Are you sure you want to delete #@item.Id?</p>
    <button class="btn btn-danger" @onclick="DeleteConfirmedAsync">Delete</button>
    <a class="btn btn-secondary ms-2" href="/{{route}}">Cancel</a>
}
""";
    }
}
