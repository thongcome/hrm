namespace Advance.SecurityCore.Web.Components.Account.Shared;

// New file — not part of HRM's original scaffolding. Replaces the hardcoded
// `@layout HRM.Components.Layout.MainLayout` in AccountLayout.razor (and the
// `@namespace HRM.Components.Layout` in Pages/Manage/_Imports.razor), both
// of which named HRM's own app-shell layout directly — something a package
// meant to be shared by three different host apps (HRM, a standalone
// Payroll product, a standalone Workflow product) cannot hardcode.
//
// A host sets this ONCE at startup (see AddAdvanceSecurityCore's comment
// about wiring this) to its own top-level layout component's Type — e.g.
// `HostLayout.LayoutType = typeof(HRM.Components.Layout.MainLayout);` — and
// AccountLayout.razor below reads it via <LayoutView Layout="...">
// instead of a compile-time @layout directive.
public static class HostLayout
{
    // Defaults to null: AccountLayout.razor then renders with no outer
    // layout at all (bare content) rather than throwing, so this package
    // still "works" (just unstyled) before a host wires a real layout in.
    public static Type? LayoutType { get; set; }
}
