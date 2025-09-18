using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class AttendanceSummary
    {
        [Key]
        public Guid SummaryId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid EmpId { get; set; } // Foreign Key to Employee

        [ForeignKey("EmpId")]
        public virtual Employee? Employee { get; set; }

        [Required]
        public Guid ComId { get; set; } // Foreign Key to Company

        [ForeignKey("ComId")] // Points to the foreign key property ComId
        public virtual Company? Company { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM}", ApplyFormatInEditMode = true)]
        public DateTime SummaryMonth { get; set; } // Summary month with proper formatting (year-month)

        [Required]
        public int TotalDays { get; set; } // Total working days in the month

        [Required]
        [Range(0, int.MaxValue)]
        public int DaysPresent { get; set; } // Number of days present

        [Required]
        [Range(0, int.MaxValue)]
        public int DaysAbsent { get; set; } // Number of days absent

        [Required]
        [Range(0, int.MaxValue)]
        public int DaysLate { get; set; } // Number of days late

        [StringLength(500)]
        public string? Remarks { get; set; } // Optional remarks
    }
}