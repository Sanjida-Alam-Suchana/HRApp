using HRApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HRApp.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Synchronous method for IQueryable
        IQueryable<T> GetAll();

        // Get all entities optionally filtered by a predicate.

        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
      
        // Get a single entity by its primary key.

        Task<T?> GetAsync(Guid id);
        // Get all entities including navigation properties, optionally filtered.
        Task<IEnumerable<T>> GetAllWithIncludeAsync(
            Expression<Func<T, bool>>? filter = null,
            params string[] includeProperties
        );

        // Get a single entity by id including navigation properties.

        Task<T?> GetWithIncludeAsync(Guid id, params string[] includeProperties);
        Task<T> GetByIdAsync(Guid id);

        
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);

        // Update an existing entity.

        Task UpdateAsync(T entity);

        // Delete an entity by its primary key.
        Task DeleteAsync(Guid id);

        // Save changes to the database.

        Task SaveAsync();

        // Find entities matching a given predicate.

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Return IQueryable for advanced querying (with Include support).

        IQueryable<T> GetQueryable();
        Task ExecuteSqlRawAsync(string sql, params object[] parameters);

        Task<IEnumerable<Salary>> GetSalariesByCompanyMonthYearAsync(Guid comId, int dtYear, int dtMonth);
        Task CalculateSalaryAsync(Guid comId, int dtYear, int dtMonth);
    }
}
