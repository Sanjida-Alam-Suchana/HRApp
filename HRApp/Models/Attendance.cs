using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Attendance
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        public Guid EmpId { get; set; }

        public DateOnly dtDate { get; set; }

        public required string AttStatus { get; set; } // P/A/L, auto-calculated

        public TimeOnly InTime { get; set; }

        public TimeOnly OutTime { get; set; }

        public virtual required Company Company { get; set; }
        public virtual required Employee Employee { get; set; }
    }
}
