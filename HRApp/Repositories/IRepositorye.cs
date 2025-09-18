using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HRApp.Repositories
{
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// Get all entities optionally filtered by a predicate.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);

        /// <summary>
        /// Get a single entity by its primary key.
        /// </summary>
        Task<T?> GetAsync(Guid id);

        /// <summary>
        /// Get all entities including navigation properties, optionally filtered.
        /// </summary>
        Task<IEnumerable<T>> GetAllWithIncludeAsync(
            Expression<Func<T, bool>>? filter = null,
            params string[] includeProperties
        );

        /// <summary>
        /// Get a single entity by id including navigation properties.
        /// </summary>
        Task<T?> GetWithIncludeAsync(Guid id, params string[] includeProperties);
        Task<T> GetByIdAsync(Guid id);
        /// <summary>
        /// Add a new entity.
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Update an existing entity.
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Delete an entity by its primary key.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Save changes to the database.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Find entities matching a given predicate.
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Return IQueryable for advanced querying (with Include support).
        /// </summary>
        IQueryable<T> GetQueryable();
        Task ExecuteSqlRawAsync(string sql, params object[] parameters);
    }
}
