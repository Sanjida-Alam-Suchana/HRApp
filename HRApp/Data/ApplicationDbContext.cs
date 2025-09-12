using Microsoft.EntityFrameworkCore;
using HRApp.Models;

namespace HRApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }
        public DbSet<Salary> Salaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>().HasKey(c => c.Id);
            modelBuilder.Entity<Designation>().HasKey(d => d.Id);
            modelBuilder.Entity<Department>().HasKey(d => d.Id);
            modelBuilder.Entity<Shift>().HasKey(s => s.Id);
            modelBuilder.Entity<Employee>().HasKey(e => e.Id);
            modelBuilder.Entity<Attendance>().HasKey(a => a.Id);
            modelBuilder.Entity<AttendanceSummary>().HasKey(a => a.Id);
            modelBuilder.Entity<Salary>().HasKey(s => s.Id);

            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmpId);
            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(a => a.Company).WithMany().HasForeignKey(a => a.ComId);

            modelBuilder.Entity<Salary>()
                .HasOne(s => s.Employee).WithMany().HasForeignKey(s => s.EmpId);
            modelBuilder.Entity<Salary>()
                .HasOne(s => s.Company).WithMany().HasForeignKey(s => s.ComId);

            modelBuilder.Entity<Employee>().Property(e => e.dtJoin).IsRequired();
        }
    }
}