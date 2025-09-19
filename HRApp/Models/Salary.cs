using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Salary
    {
        [Key]
        public Guid SalaryId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EmpId { get; set; } // FK to Employee

        [ForeignKey("EmpId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        public Guid ComId { get; set; } // FK to Company

        [ForeignKey("ComId")]
        public virtual Company? Company { get; set; }

        [Required]
        [Range(2000, 2100)]
        public int dtYear { get; set; } // Year of salary

        [Required]
        [Range(1, 12)]
        public int dtMonth { get; set; } // Month of salary (1-12)

        [NotMapped]
        public string SalaryMonth => $"{dtYear}-{dtMonth:D2}"; // Formatted as yyyy-MM

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Gross { get; set; } // Gross salary

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Basic { get; set; } // Basic salary

        [Required]
        [Range(0, double.MaxValue)]
        public decimal HRent { get; set; } // House rent

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Medical { get; set; } // Medical allowance

        [Range(0, 31)]
        public int AbsentDays { get; set; } // Attendance summary data

        [Required]
        [Range(0, double.MaxValue)]
        public decimal AbsentAmount { get; set; } // Calculated Absent Amount

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PayableAmount { get; set; } // Calculated Payable Amount

        public bool IsPaid { get; set; } = false; // Paid or not

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; } = 0; // Final paid amount
    }
}