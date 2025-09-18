using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public enum GenderType
    {
        Male = 1,
        Female = 2,
        Other = 3
    }

    public class Employee
    {
        [Key]
        public Guid EmpId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(20)]
        public string EmpCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EmpName { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Company")]
        public Guid ComId { get; set; }
        public virtual Company Company { get; set; } = null!;

        [Required]
        [ForeignKey("Shift")]
        public Guid ShiftId { get; set; }
        public virtual Shift Shift { get; set; } = null!;

        [Required]
        [ForeignKey("Department")]
        public Guid DeptId { get; set; }
        public virtual Department Department { get; set; } = null!;

        [Required]
        [ForeignKey("Designation")]
        public Guid DesigId { get; set; }
        public virtual Designation Designation { get; set; } = null!;

        [Required]
        public GenderType Gender { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Gross salary must be greater than 0")]
        public decimal Gross { get; set; }

        [Required]
        public DateTime DtJoin { get; set; } = DateTime.UtcNow;

        // Now persistable properties with getters and setters
        [Column(TypeName = "decimal(18,2)")]
        public decimal Basic { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Medical { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Others { get; set; }

        public virtual ICollection<Salary>? Salaries { get; set; }
        public virtual ICollection<Attendance>? Attendances { get; set; }
        public virtual ICollection<AttendanceSummary>? AttendanceSummaries { get; set; }
    }
}