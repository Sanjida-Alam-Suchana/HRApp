using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Shift
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        [Required]
        public required string ShiftName { get; set; }

        public TimeOnly In { get; set; }

        public TimeOnly Out { get; set; }

        public TimeOnly Late { get; set; }

        public virtual required Company Company { get; set; }
        public virtual required ICollection<Employee> Employees { get; set; }
    }
}
