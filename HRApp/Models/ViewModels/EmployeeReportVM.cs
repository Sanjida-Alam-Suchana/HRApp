using System;

namespace HRApp.Models.ViewModels
{
    public class EmployeeReportVM
    {
        public string EmployeeName { get; set; }
        public DateTime JoinDate { get; set; }
        public int ServiceDays { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }
        public string ShiftName { get; set; }
    }
}