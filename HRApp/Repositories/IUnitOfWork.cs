using HRApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRApp.Repositories
{
    public interface IUnitOfWork : IDisposable
    {

        IRepository<Company> Companies { get; }
        IRepository<Designation> Designations { get; }
        IRepository<Department> Departments { get; }
        IRepository<Shift> Shifts { get; }
        IRepository<Employee> Employees { get; }
        IRepository<Attendance> Attendances { get; }
        IRepository<Salary> Salaries { get; }
        IRepository<AttendanceSummary> AttendanceSummaries { get; }
       
        DbContext DbContext { get; }

        Task ExecRawAsync(string sql, Guid comId, int year, int month);
        Task<int> SaveAsync();

        Task<IEnumerable<T>> ExecuteStoredProcedure<T>(string storedProcedureName, object parameters) where T : class;
        IQueryable<Employee> GetEmployeeQueryable();
       
    }
}