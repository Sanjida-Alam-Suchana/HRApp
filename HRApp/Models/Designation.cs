using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Designation
    {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        [Required]
        public required string DesigName { get; set; }

        public virtual required Company Company { get; set; }
        public virtual required ICollection<Employee> Employees { get; set; }
    }
}
