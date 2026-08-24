namespace HRM.Services.Security;

using System.Security.Claims;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

// Why this is an explicit opt-in call per page rather than a transparent
// replacement for every existing "await DbFactory.CreateDbContextAsync()"
// in the app (which is what "one global query filter, automatic
// everywhere" would ideally mean):
//
// 1. AddDbContextFactory<HRMContext>() registers IDbContextFactory<HRMContext>
//    as a SINGLETON (documented EF Core default — the factory itself must
//    outlive any single Blazor circuit). A Singleton resolves any extra
//    constructor dependency from the ROOT service provider, not a
//    per-circuit scope, so injecting a Scoped "current user" into
//    HRMContext's own constructor throws "Cannot resolve scoped service
//    ... from root provider" — the exact failure already hit and
//    documented in HRMContextFactory.cs/ApplicationDbContextFactory.cs for
//    an unrelated reason. Setting CurrentScope on the instance AFTER
//    construction (a plain settable property, see HRMContext.Security.cs)
//    sidesteps that.
//
// 2. That still leaves "how does CurrentScope get set for the ~150 pages
//    that already call DbFactory.CreateDbContextAsync() directly, without
//    editing all of them?" The answer turns out to be: it can't, cleanly.
//    IDbContextFactory<T>.CreateDbContextAsync() is not an interface member
//    — it's a fixed EF Core extension method that always wraps the
//    synchronous CreateDbContext(), which can't safely await claims
//    resolution (blocking on that Task risks a sync-over-async deadlock).
//    Replacing the DI registration for IDbContextFactory<HRMContext>
//    wholesale can't work around this without either blocking synchronously
//    or restructuring HRMContext's registration away from the
//    factory-per-instance pattern the whole app depends on — both bigger,
//    riskier changes than this slice's "don't touch existing screens" goal
//    allows.
//
// So: pages opt in explicitly, one line, same DbFactory they already
// inject. Rollout to more pages happens gradually; every page that hasn't
// been touched keeps behaving exactly as before (CurrentScope stays at its
// default, Unrestricted).
public static class ScopedDbContextExtensions
{
    public static async Task<HRMContext> CreateScopedDbContextAsync(
        this IDbContextFactory<HRMContext> factory, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var context = await factory.CreateDbContextAsync(ct);
        context.CurrentScope = RoleScopeSnapshot.FromClaims(user);
        return context;
    }
}
