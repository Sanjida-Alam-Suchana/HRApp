using HRApp.Data;
using HRApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HRApp.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }
        public IQueryable<T> GetAll() => _dbSet;
        public async Task<T?> GetAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        {
            if (filter != null)
                return await _dbSet.Where(filter).ToListAsync();
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(Expression<Func<T, bool>>? filter = null, params string[] includeProperties)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
                query = query.Where(filter);
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
            return await query.ToListAsync();
        }

        public async Task<T?> GetWithIncludeAsync(Guid id, params string[] includeProperties)
        {
            IQueryable<T> query = _dbSet;
            foreach (var includeProperty in includeProperties)
                query = query.Include(includeProperty);
            return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await GetAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public async Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            await SaveAsync(); // Ensure changes are saved
        }
        public async Task ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            await _context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public async Task<IEnumerable<Salary>> GetSalariesByCompanyMonthYearAsync(Guid comId, int dtYear, int dtMonth)
        {
            return await _context.Salaries
                .Where(s => s.ComId == comId && s.dtYear == dtYear && s.dtMonth == dtMonth)
                .Include(s => s.Employee)
                .ToListAsync();
        }

        
        public async Task CalculateSalaryAsync(Guid comId, int dtYear, int dtMonth)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "CALL CalculateSalary({0}, {1}, {2})",
                comId, dtYear, dtMonth
            );
        }
    }
}