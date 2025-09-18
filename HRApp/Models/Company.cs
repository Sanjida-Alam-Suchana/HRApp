using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Company
    {
        [Key]
        public Guid ComId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public required string ComName { get; set; }

        [Required]
        [Range(0, 1)]
        public decimal Basic { get; set; } // e.g., 0.5 for 50%

        [Required]
        [Range(0, 1)]
        public decimal Hrent { get; set; } // e.g., 0.3 for 30%

        [Required]
        [Range(0, 1)]
        public decimal Medical { get; set; } // e.g., 0.15 for 15%

        public bool IsInactive { get; set; } = false;
        // One-to-Many relationships
        public virtual ICollection<Designation> Designations { get; set; } = new List<Designation>();
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();
        public virtual ICollection<AttendanceSummary> AttendanceSummaries { get; set; } = new List<AttendanceSummary>();
    }
}


