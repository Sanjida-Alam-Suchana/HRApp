using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Designation
    {
        [Key]
        public Guid DesigId { get; set; } = Guid.NewGuid();

        [ForeignKey("Company")] // Specifies that ComId is a Foreign Key referencing Company.ComId
        public Guid ComId { get; set; } // Foreign Key to Company

        [Required]
        [StringLength(100)]
        public required string DesigName { get; set; }

        // Navigation property
        public virtual Company Company { get; set; }
    }
}

