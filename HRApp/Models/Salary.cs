using System.ComponentModel.DataAnnotations;

namespace HRApp.Models
{
    public class Salary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ComId { get; set; }

        public Guid EmpId { get; set; }

        public int dtYear { get; set; }

        public int dtMonth { get; set; }

        public decimal Gross { get; set; }

        public decimal Basic { get; set; }

        public decimal HRent { get; set; }

        public decimal Medical { get; set; }

        public decimal AbsentAmount { get; set; }

        public decimal PayableAmount { get; set; }

        public bool IsPaid { get; set; } = false;

        public decimal PaidAmount { get; set; } = 0;

        public virtual required Company Company { get; set; }
        public virtual required Employee Employee { get; set; }
    }
}
