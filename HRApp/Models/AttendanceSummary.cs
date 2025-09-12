using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class AttendanceSummary
    {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmpId { get; set; }

        public Guid ComId { get; set; }

        public int dtYear { get; set; }

        public int dtMonth { get; set; }

        public int Present { get; set; }

        public int Late { get; set; }

        public int Absent { get; set; }

        public virtual required Employee Employee { get; set; }
        public virtual required Company Company { get; set; }
    }
}
