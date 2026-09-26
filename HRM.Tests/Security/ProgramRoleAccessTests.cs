using HRM.Models;
using HRM.Services.Security;
using Xunit;
using Rights = HRM.Services.Security.ProgramRoleService.ProgramRights;

namespace HRM.Tests.Security;

// AD.CRUDManage enforcement (see the advance-crud-manage skill) is fail-closed
// per-action access control: every write handler calls RequireAsync, which
// leans entirely on the pure resolution extracted here from GetRightsAsync.
// The two things that must never regress are the SEGMENT-boundary prefix match
// ("/admin" must not grant "/administrators") and the fail-closed default (a
// path no row covers grants nothing). These tests pin both, plus the
// longest-prefix and cross-role OR rules the real check depends on.
public class ProgramRoleAccessTests
{
    private static sc_program_role Row(string path, bool c = false, bool r = false, bool e = false, bool d = false)
        => new() { progpath = path, cancreate = c, canread = r, canedit = e, candelete = d };

    private static IReadOnlyList<sc_program_role>[] OneRole(params sc_program_role[] rows)
        => new IReadOnlyList<sc_program_role>[] { rows };

    // ---- path normalization ----

    [Theory]
    [InlineData("/leave-requests/", "/leave-requests")]
    [InlineData("/leave-requests", "/leave-requests")]
    [InlineData("/", "/")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void NormalizePath_strips_trailing_slash_and_defaults_to_root(string? input, string expected)
        => Assert.Equal(expected, ProgramRoleService.NormalizePath(input));

    // ---- boundary-respecting prefix match ----

    [Theory]
    [InlineData("/admin", "/admin", true)]         // exact
    [InlineData("/admin", "/admin/users", true)]   // child segment
    [InlineData("/admin", "/administrators", false)] // sibling that merely shares a prefix — the security case
    [InlineData("/admin", "/adm", false)]          // shorter
    [InlineData("/", "/anything/here", true)]      // root covers everything
    [InlineData("/pay/runs", "/pay/runsheet", false)] // boundary mid-path
    [InlineData("/pay/runs", "/pay/runs/5/edit", true)]
    public void ProgPathCovers_requires_a_segment_boundary(string progpath, string path, bool covered)
        => Assert.Equal(covered, ProgramRoleService.ProgPathCovers(progpath, path));

    // ---- fail-closed ----

    [Fact]
    public void No_matching_row_yields_none()
    {
        var rows = OneRole(Row("/pay", r: true, e: true));
        Assert.Equal(Rights.None, ProgramRoleService.ResolveRights(rows, "/leave-requests"));
    }

    [Fact]
    public void No_roles_at_all_yields_none()
        => Assert.Equal(Rights.None, ProgramRoleService.ResolveRights(System.Array.Empty<IReadOnlyList<sc_program_role>>(), "/pay"));

    [Fact]
    public void A_prefix_sibling_does_not_leak_rights()
    {
        // Row grants edit on /admin; a request to /administrators must get
        // nothing, not inherit /admin's grant.
        var rows = OneRole(Row("/admin", r: true, e: true, d: true));
        Assert.Equal(Rights.None, ProgramRoleService.ResolveRights(rows, "/administrators"));
    }

    // ---- longest-prefix within a role ----

    [Fact]
    public void Longest_prefix_wins_more_specific_row_overrides_broader_one()
    {
        // Broad grant edits all of /leave-requests, but a specific read-only
        // row on /leave-requests/policy must WIN for that path (only read).
        var rows = OneRole(
            Row("/leave-requests", r: true, e: true, d: true),
            Row("/leave-requests/policy", r: true));
        var rights = ProgramRoleService.ResolveRights(rows, "/leave-requests/policy");
        Assert.Equal(new Rights(false, true, false, false), rights);
    }

    [Fact]
    public void Broader_row_still_applies_to_paths_the_specific_row_does_not_cover()
    {
        var rows = OneRole(
            Row("/leave-requests", r: true, e: true),
            Row("/leave-requests/policy", r: true));
        var rights = ProgramRoleService.ResolveRights(rows, "/leave-requests/detail");
        Assert.Equal(new Rights(false, true, true, false), rights);
    }

    // ---- OR across roles ----

    [Fact]
    public void Rights_accumulate_across_multiple_roles()
    {
        // Role A: read+edit on /pay. Role B: create+delete on /pay. Combined
        // user should hold all four.
        var perRole = new IReadOnlyList<sc_program_role>[]
        {
            new[] { Row("/pay", r: true, e: true) },
            new[] { Row("/pay", c: true, d: true) },
        };
        Assert.Equal(new Rights(true, true, true, true), ProgramRoleService.ResolveRights(perRole, "/pay"));
    }

    [Fact]
    public void Broad_edit_on_one_role_is_not_masked_by_a_narrow_readonly_on_another()
    {
        // The reason resolution is per-role-then-OR, not flatten-then-longest-
        // prefix: role A grants edit broadly on /x; role B has a narrower
        // read-only row on /x/y. For /x/y the user must still get A's edit —
        // a flattened longest-prefix would pick B's row alone and lose it.
        var perRole = new IReadOnlyList<sc_program_role>[]
        {
            new[] { Row("/x", r: true, e: true) },
            new[] { Row("/x/y", r: true) },
        };
        var rights = ProgramRoleService.ResolveRights(perRole, "/x/y");
        Assert.True(rights.CanEdit, "broad edit grant on another role was masked");
        Assert.True(rights.CanRead);
    }

    // ---- role name → role ids (the cached snapshot) ----

    private static System.Security.Claims.ClaimsPrincipal UserInRoles(params string[] roles)
        => new(new System.Security.Claims.ClaimsIdentity(
            roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)), "test"));

    private static sc_program_role RoleRow(long roleId, string path, bool r = false, bool e = false)
        => new() { roleid = roleId, progpath = path, canread = r, canedit = e };

    [Fact]
    public void Two_active_roles_with_the_same_name_do_not_throw_and_both_count()
    {
        // Used to be ToDictionaryAsync(name → roleid): a duplicate name threw on
        // every rights check, so every page failed for everyone.
        var rows = new[] { RoleRow(1, "/pay", r: true), RoleRow(2, "/pay", e: true) };
        var roles = new[] { ("PAYROLL_OFFICER", 1L), ("payroll_officer", 2L) };

        var rights = ProgramRoleService.RightsFromRows(rows, roles, UserInRoles("Payroll_Officer"), "/pay/runs");

        Assert.True(rights.CanRead);
        Assert.True(rights.CanEdit);
    }

    [Fact]
    public void Role_name_with_no_active_role_grants_nothing()
    {
        var rows = new[] { RoleRow(1, "/pay", r: true, e: true) };
        var roles = new[] { ("Admin", 1L) };

        Assert.Equal(Rights.None, ProgramRoleService.RightsFromRows(rows, roles, UserInRoles("Ghost"), "/pay"));
    }
}
