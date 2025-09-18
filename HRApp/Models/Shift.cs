using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Shift
    {
        [Key]
        public Guid ShiftId { get; set; } = Guid.NewGuid();

        [ForeignKey("Company")] // Declare ComId as Foreign Key referencing Company.ComId
        public Guid ComId { get; set; } // Foreign Key to Company

        [Required]
        [StringLength(100)]
        public required string ShiftName { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        // Navigation property
        public virtual Company Company { get; set; }
    }
}