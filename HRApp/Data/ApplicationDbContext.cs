using HRApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Company> Companies { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company entity
            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(e => e.Basic).HasPrecision(18, 2);
                entity.Property(e => e.Hrent).HasPrecision(18, 2);
                entity.Property(e => e.Medical).HasPrecision(18, 2);
            });

            // Configure Employee entity
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.Gross).HasPrecision(18, 2);
                entity.Property(e => e.DtJoin).HasColumnType("timestamp without time zone");
                // Configure new properties
                entity.Property(e => e.Basic).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.HRent).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Medical).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Others).HasPrecision(18, 2).IsRequired();
            });

            // Configure Salary entity
            modelBuilder.Entity<Salary>(entity =>
            {
                entity.Property(e => e.Basic).HasPrecision(18, 2);
                entity.Property(e => e.Gross).HasPrecision(18, 2);
                entity.Property(e => e.Hrent).HasPrecision(18, 2);
                entity.Property(e => e.Medical).HasPrecision(18, 2);
                entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            });

            // Foreign Key relationships
            modelBuilder.Entity<Designation>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Designations)
                .HasForeignKey(d => d.ComId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.ComId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Shifts)
                .HasForeignKey(s => s.ComId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.ComId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Designation)
                .WithMany()
                .HasForeignKey(e => e.DesigId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DeptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Shift)
                .WithMany()
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Salary>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Salaries)
                .HasForeignKey(s => s.EmpId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(asum => asum.Employee)
                .WithMany(e => e.AttendanceSummaries)
                .HasForeignKey(asum => asum.EmpId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}