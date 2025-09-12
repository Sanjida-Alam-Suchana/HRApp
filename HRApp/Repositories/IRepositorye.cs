using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Repositories
{
    public interface IRepository<T> where T : class
    {
        T GetById(Guid id);
        IEnumerable<T> GetAll();
        IQueryable<T> GetAllQueryable();  
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Save();

        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task SaveAsync();
    }
}