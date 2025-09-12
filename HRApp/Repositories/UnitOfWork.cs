using HRApp.Data;
using HRApp.Models;
using System;
using System.Threading.Tasks;

namespace HRApp.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public IRepository<Company> Companies { get; private set; }
        public IRepository<Designation> Designations { get; private set; }
        public IRepository<Department> Departments { get; private set; }
        public IRepository<Shift> Shifts { get; private set; }
        public IRepository<Employee> Employees { get; private set; }
        public IRepository<Attendance> Attendances { get; private set; }
        public IRepository<AttendanceSummary> AttendanceSummaries { get; private set; }
        public IRepository<Salary> Salaries { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Companies = new Repository<Company>(_context);
            Designations = new Repository<Designation>(_context);
            Departments = new Repository<Department>(_context);
            Shifts = new Repository<Shift>(_context);
            Employees = new Repository<Employee>(_context);
            Attendances = new Repository<Attendance>(_context);
            AttendanceSummaries = new Repository<AttendanceSummary>(_context);
            Salaries = new Repository<Salary>(_context);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}