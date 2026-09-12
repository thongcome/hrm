namespace Advance.SecurityCore.Services;

// Replaces HRM's static Services/Security/ScMenuNavCatalog.cs list. That
// catalog hardcoded ALL of HRM's ~30 drawer groups and every link under
// them as one static class — fine for a single product, but SecurityCore
// is meant to seed sc_menu for THREE different products (HRM, Payroll,
// Workflow) that each own a different slice of the menu tree. A host module
// registers one of these per menu-owning module (e.g. HRM's Pay_* module
// registers its own contributor, Payroll-as-a-standalone-product registers
// its own, etc.) via DI (`services.AddSingleton<IMenuNavContributor, X>()`)
// and ScMenuNavSeeder below aggregates every registered contributor's
// Groups/Links at seed time — instead of one giant list a single team has
// to keep in sync with every module.
public interface IMenuNavContributor
{
    IReadOnlyList<NavCatalogGroup> Groups { get; }
    IReadOnlyList<NavCatalogEntry> Links { get; }
}

// Same shape as HRM's ScMenuNavCatalog record types — copied verbatim so a
// host's existing catalog data (e.g. HRM's ScMenuNavCatalog.cs, ~400 lines
// of Groups/Links) can become the body of an IMenuNavContributor
// implementation with no changes beyond wrapping it in a class + interface.
public record NavCatalogEntry(
    string? GroupCode,   // null for top-level links; else the synthetic group code this link belongs to
    string? Code,        // gate menucode (innermost Has()/HasAny(); first code for HasAny); null = renders for every logged-in user
    string Url,          // Href verbatim
    string NameTh,
    string NameEn,
    string Icon,         // e.g. "Icons.Material.Filled.EventBusy" (stored as plain string)
    int Order);          // running order within its group (10, 20, 30...)

public record NavCatalogGroup(
    string GroupCode,    // synthetic, stable: "GRP_" + short latin slug
    string NameTh,
    string NameEn,
    string Icon,
    int Order);          // order of the group in the drawer (10, 20, ...)
