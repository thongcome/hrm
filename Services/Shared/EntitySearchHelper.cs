namespace HRM.Services.Shared;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

// Reflection + expression-tree based "search across any field" — the "AI
// search" behavior requested for the CRUD scaffold is really just a LIKE
// %term% across several named string columns, OR'd together, translated to
// real SQL (not client-eval) via EF.Functions.Like so it stays index-usable
// and doesn't pull the whole table into memory first.
public static class EntitySearchHelper
{
    public static IQueryable<T> ApplyTextSearch<T>(IQueryable<T> query, string? term, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(term) || propertyNames.Length == 0)
            return query;

        var param = Expression.Parameter(typeof(T), "e");
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;
        var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);
        var pattern = Expression.Constant($"%{term}%");

        Expression? combined = null;
        foreach (var propName in propertyNames)
        {
            var prop = typeof(T).GetProperty(propName);
            if (prop is null || prop.PropertyType != typeof(string))
                continue; // silently skip — a typo'd/renamed property name should narrow the search, not crash the page

            var propAccess = Expression.Property(param, prop);
            var likeCall = Expression.Call(likeMethod, efFunctions, propAccess, pattern);
            combined = combined is null ? likeCall : Expression.OrElse(combined, likeCall);
        }

        return combined is null ? query : query.Where(Expression.Lambda<Func<T, bool>>(combined, param));
    }

    // Several legacy wf_* tables (verified: wf_org_type, wf_employee, wf_checklist,
    // wf_customer_approver — checked via sys.columns.is_identity) have a `long id` PK
    // that is NOT an actual IDENTITY column in the DB, despite the EF scaffold guessing
    // [DatabaseGenerated(Identity)] originally. For those tables the app must assign id
    // itself before insert, or every Create after the first collides on id=0. Check
    // sys.columns.is_identity for a table before assuming CrudScaffold's default
    // "entity.id == 0 => Add" heuristic is safe — if it isn't, call this in SaveAsync
    // before adding the new entity. Best-effort MAX+1 (not a DB sequence): acceptable
    // for these low-traffic internal admin tables, not safe under heavy concurrent creates.
    public static async Task<long> NextIdAsync<T>(IQueryable<T> query, Expression<Func<T, long>> idSelector)
    {
        var maxId = await query.Select(idSelector).OrderByDescending(x => x).FirstOrDefaultAsync();
        return maxId + 1;
    }
}
