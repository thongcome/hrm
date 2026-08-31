using HRM.Services.Security;
using Xunit;

namespace HRM.Tests.Security;

// AD.CRUDManage safety net (see the advance-crud-manage skill): the
// auto-seeder's route scanner is the only thing standing between "a page
// exists" and "a permission row exists" — a route shape it can't parse
// would silently leave that page ungoverned. These tests make that failure
// loud at build time instead of discovering it in production.
public class ProgramRoleSeederTests
{
    [Fact]
    public void Every_routed_page_normalizes_to_a_valid_program_path()
    {
        var paths = ProgramRoleService.ScanRoutedPaths();

        Assert.All(paths, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p));
            Assert.StartsWith("/", p);
            // No parameter braces may survive normalization — a brace here
            // means a template shape the normalizer didn't strip.
            Assert.DoesNotContain("{", p);
            Assert.True(p == "/" || !p.EndsWith('/'), $"trailing slash survived: {p}");
        });
    }

    [Fact]
    public void Scanner_actually_sees_the_app_not_an_empty_assembly()
    {
        // This codebase has well over a hundred routed pages; if this count
        // ever collapses the scanner is looking at the wrong assembly or
        // RouteAttribute discovery broke — both would silently gut the
        // permission seeding.
        var paths = ProgramRoleService.ScanRoutedPaths();
        Assert.True(paths.Count > 100, $"expected >100 routed paths, scanner found {paths.Count}");
    }

    [Theory]
    [InlineData("/leave-requests/detail/{Id:long}", "/leave-requests/detail")]
    [InlineData("/leave-requests", "/leave-requests")]
    [InlineData("/job/profiles/{PosExecTypeId:long}", "/job/profiles")]
    [InlineData("/", "/")]
    [InlineData("/{param}", "/")]
    [InlineData("/eng/campaigns/{Id:long}/results", "/eng/campaigns")]
    public void Normalizer_strips_parameters_and_keeps_stable_prefixes(string template, string expected)
    {
        Assert.Equal(expected, ProgramRoleService.NormalizeRouteTemplate(template));
    }
}
