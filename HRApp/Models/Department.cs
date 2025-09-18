using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRApp.Models
{
    public class Department
    {
        [Key]
        public Guid DeptId { get; set; } = Guid.NewGuid();

        [ForeignKey("Company")] // Declare ComId as Foreign Key referencing Company.ComId
        public Guid ComId { get; set; } // Foreign Key to Company

        [Required]
        [StringLength(100)]
        public required string DeptName { get; set; }

        // Navigation property
        public virtual Company Company { get; set; }
    }
}

