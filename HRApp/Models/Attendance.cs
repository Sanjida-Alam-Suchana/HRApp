using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Attendance
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        public Guid EmpId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly dtDate { get; set; }

        [Required]
        public string AttStatus { get; set; } // P/A/L, auto-calculated

        [Required]
        public TimeOnly InTime { get; set; }

        [Required]
        public TimeOnly OutTime { get; set; }

        [ForeignKey("ComId")]
        public virtual required Company Company { get; set; }

        [ForeignKey("EmpId")]
        public virtual required Employee Employee { get; set; }
    }
}

