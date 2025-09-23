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
        private string _attStatus;

        public string AttStatus
        {
            get => _attStatus; // Returns the stored value
            set => _attStatus = value;
        } // P/A/L, auto-calculated

        [Required]
        public TimeOnly InTime { get; set; }

        [Required]
        public TimeOnly OutTime { get; set; }

        [ForeignKey("ComId")]
        public virtual required Company Company { get; set; }

        [ForeignKey("EmpId")]
        public virtual required Employee Employee { get; set; }

        public void RecalculateStatus()
        {
            _attStatus = CalculateAttStatus();
            System.Diagnostics.Debug.WriteLine($"Recalculated AttStatus: {_attStatus}, InTime: {InTime}, OutTime: {OutTime}, Shift.StartTime: {Employee?.Shift?.StartTime}");
        }

        private string CalculateAttStatus()
        {
            System.Diagnostics.Debug.WriteLine($"Calculating AttStatus - InTime: {InTime}, OutTime: {OutTime}, Shift.StartTime: {Employee?.Shift?.StartTime}");
            if (InTime == TimeOnly.MinValue || OutTime == TimeOnly.MinValue)
                return "A";
            var shift = Employee?.Shift;
            if (shift == null)
            {
                System.Diagnostics.Debug.WriteLine("Shift is null for Employee ID: " + Employee?.EmpId);
                return "A";
            }
            // Match controller's logic with 15-minute grace period
            var graceMinutes = 15;
            if (InTime > shift.StartTime.Add(TimeSpan.FromMinutes(graceMinutes)))
                return "L";
            return "P";
        }
    }
}