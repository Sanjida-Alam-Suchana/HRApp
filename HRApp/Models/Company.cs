using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string ComName { get; set; }

        [Required]
        public decimal Basic { get; set; } // Percentage, e.g., 50 for 50%

        [Required]
        public decimal HRent { get; set; } // Percentage

        [Required]
        public decimal Medical { get; set; } // Percentage

        public bool IsInactive { get; set; } = false;

        public virtual required ICollection<Designation> Designations { get; set; }
        public virtual required ICollection<Department> Departments { get; set; }
        public virtual required ICollection<Shift> Shifts { get; set; }
        public virtual required ICollection<Employee> Employees { get; set; }
        public virtual required ICollection<Attendance> Attendances { get; set; }
        public virtual required ICollection<AttendanceSummary> AttendanceSummaries { get; set; }
        public virtual required ICollection<Salary> Salaries { get; set; }
    }
}
