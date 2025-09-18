using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using HRApp.Data;
using HRApp.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private bool _disposed = false;

        private IRepository<Company>? _companies;
        private IRepository<Designation>? _designations;
        private IRepository<Department>? _departments;
        private IRepository<Shift>? _shifts;
        private IRepository<Employee>? _employees;
        private IRepository<Attendance>? _attendances;
        private IRepository<Salary>? _salaries;
        private IRepository<AttendanceSummary>? _attendanceSummaries;
        public DbContext DbContext => _context;

        public IRepository<Company> Companies => _companies ??= new Repository<Company>(_context);
        public IRepository<Designation> Designations => _designations ??= new Repository<Designation>(_context);
        public IRepository<Department> Departments => _departments ??= new Repository<Department>(_context);
        public IRepository<Shift> Shifts => _shifts ??= new Repository<Shift>(_context);
        public IRepository<Employee> Employees => _employees ??= new Repository<Employee>(_context);
        public IRepository<Attendance> Attendances => _attendances ??= new Repository<Attendance>(_context);
        public IRepository<Salary> Salaries => _salaries ??= new Repository<Salary>(_context);
        public IRepository<AttendanceSummary> AttendanceSummaries => _attendanceSummaries ??= new Repository<AttendanceSummary>(_context);
        public IEmployeeRepository Employee { get; private set; }
        public IDepartmentRepository Department { get; private set; }
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            //Employee = new EmployeeRepository(_context);
            Department = new DepartmentRepository(_context);
        }

        public async Task<int> SaveAsync() // Changed from void to Task<int>
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> ExecuteStoredProcedure<T>(string storedProcedureName, object parameters) where T : class
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
            var results = new List<T>();
            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(storedProcedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    if (parameters != null)
                    {
                        var properties = parameters.GetType().GetProperties();
                        foreach (var prop in properties)
                        {
                            command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(parameters) ?? DBNull.Value);
                        }
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = Activator.CreateInstance<T>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(T).GetProperty(reader.GetName(i));
                                if (prop != null && !reader.IsDBNull(i))
                                {
                                    prop.SetValue(item, reader.GetValue(i));
                                }
                            }
                            results.Add(item);
                        }
                    }
                }
            }
            return results;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public IQueryable<Employee> GetEmployeeQueryable()
        {
            return _context.Employees.AsQueryable();
        }
        public async Task ExecRawAsync(string sql, Guid comId, int year, int month)
        {
            await _context.Database.ExecuteSqlRawAsync(sql, comId, year, month);
        }

    }
}