using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Employee
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        [Required]
        [StringLength(50)]
        public required string EmpCode { get; set; }

        [Required]
        public required string EmpName { get; set; }

        public Guid ShiftId { get; set; }

        public Guid DeptId { get; set; }

        public Guid DesigId { get; set; }

        [Required]
        public required string Gender { get; set; }

        [Required]
        public decimal Gross { get; set; }

        public decimal Basic { get; set; }

        public decimal HRent { get; set; }

        public decimal Medical { get; set; }

        public decimal Others { get; set; } = 0;

        public DateTime dtJoin { get; set; }

        public virtual required Company Company { get; set; }
        public virtual required Shift Shift { get; set; }
        public virtual required Department Department { get; set; }
        public virtual required Designation Designation { get; set; }
        public virtual required ICollection<Attendance> Attendances { get; set; }
        public virtual required ICollection<AttendanceSummary> AttendanceSummaries { get; set; }
        public virtual required ICollection<Salary> Salaries { get; set; }
    }
}
