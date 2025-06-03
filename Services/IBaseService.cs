using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System;
using HRM.Models;

namespace HRM.Services
{
    public class BaseService<T> : IBaseService<T>, ISearchableService<T> where T : class
    {
        private readonly HRMContext _context;
        private readonly ICompanyContext _companyContext;
        private readonly DbSet<T> _dbSet;

        public BaseService(HRMContext context, ICompanyContext companyContext)
        {
            _context = context;
            _companyContext = companyContext;
            _dbSet = _context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet
                .Where(HasCompanyIdEqual())
                .ToListAsync();
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            return await _dbSet
                .Where(HasCompanyIdEqual())
                .FirstOrDefaultAsync(e => EF.Property<long>(e, "Id") == id);
        }

        public async Task AddAsync(T entity)
        {
            SetCompanyId(entity);
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        // 🔒 Filter by company_id
        private Expression<Func<T, bool>> HasCompanyIdEqual()
        {
            var param = Expression.Parameter(typeof(T), "x");
            var companyProp = Expression.Property(param, "company_id");
            var companyValue = Expression.Constant(_companyContext.CompanyId);
            var equal = Expression.Equal(companyProp, companyValue);
            return Expression.Lambda<Func<T, bool>>(equal, param);
        }

        // 🛠 Set company_id before Add
        private void SetCompanyId(T entity)
        {
            var prop = typeof(T).GetProperty("company_id");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(entity, _companyContext.CompanyId);
            }
        }

        public async Task<List<T>> SearchAsync(string keyword, params string[] fields)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            var param = Expression.Parameter(typeof(T), "x");
            Expression? predicate = null;

            foreach (var field in fields)
            {
                var property = Expression.Property(param, field);
                var toStringCall = Expression.Call(property, "ToString", null, null);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                var keywordExpr = Expression.Constant(keyword, typeof(string));
                var containsExpr = Expression.Call(toStringCall, containsMethod, keywordExpr);

                predicate = predicate == null ? containsExpr : Expression.OrElse(predicate, containsExpr);
            }

            // Add company_id filter
            var companyProp = Expression.Property(param, "company_id");
            var companyValue = Expression.Constant(_companyContext.CompanyId);
            var companyCheck = Expression.Equal(companyProp, companyValue);

            var finalExpr = Expression.AndAlso(companyCheck, predicate!);

            var lambda = Expression.Lambda<Func<T, bool>>(finalExpr, param);

            return await _dbSet.Where(lambda).ToListAsync();
        }

    }

}
