using HRApp.Data;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;



namespace HRApp.Controllers
{
    public class AttendancesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AttendancesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> AttendanceIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;
           // ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync();

            var selectedComId = Request.Cookies["SelectedComId"];

            // Use 'var' and AsQueryable() to avoid type conflicts
            var query = _unitOfWork.Attendances.GetQueryable()
                .Include(a => a.Employee)
                .Include(a => a.Company)
                .AsQueryable();

            if (!string.IsNullOrEmpty(selectedComId))
            {
                query = query.Where(a => a.ComId.ToString() == selectedComId);
            }

            var attendances = await query.ToListAsync();
            return View(attendances);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceCreate(Attendance attendance)
        {
            if (attendance.EmpId == Guid.Empty || attendance.ComId == Guid.Empty)
                return Json(new { success = false, message = "Employee and Company are required." });

            var employee = await _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmpId == attendance.EmpId);

            if (employee == null) return Json(new { success = false, message = "Employee not found." });

            attendance.Id = Guid.NewGuid();
            attendance.AttStatus = (employee.Shift != null && attendance.InTime > employee.Shift.StartTime) ? "L" : "P";
            attendance.Employee = employee;
            attendance.Company = employee.Company;

            await _unitOfWork.Attendances.AddAsync(attendance);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Attendance created!",
                attendance = new
                {
                    id = attendance.Id,
                    empName = attendance.Employee.EmpName,
                    date = attendance.dtDate.ToString("yyyy-MM-dd"),
                    inTime = attendance.InTime.ToString("HH:mm"),
                    outTime = attendance.OutTime.ToString("HH:mm"),
                    attStatus = attendance.AttStatus
                }
            });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceEdit(Guid id, Attendance attendance)
        {
            var existing = await _unitOfWork.Attendances.GetQueryable()
                .Include(a => a.Employee)
                .Include(a => a.Company)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existing == null) return Json(new { success = false, message = "Attendance not found." });

            var employee = await _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmpId == attendance.EmpId);

            if (employee == null) return Json(new { success = false, message = "Employee not found." });

            existing.EmpId = attendance.EmpId;
            existing.ComId = employee.ComId;
            existing.dtDate = attendance.dtDate;
            existing.InTime = attendance.InTime;
            existing.OutTime = attendance.OutTime;
            existing.AttStatus = (employee.Shift != null && attendance.InTime > employee.Shift.StartTime) ? "L" : "P";
            existing.Employee = employee;
            existing.Company = employee.Company;

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Attendance updated!", attStatus = existing.AttStatus });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attendance = await _unitOfWork.Attendances.GetAsync(id);
            if (attendance == null) return Json(new { success = false, message = "Attendance not found." });

            await _unitOfWork.Attendances.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Attendance deleted!" });
        }

        // Bulk attendance model
        public class BulkAttendanceModel
        {
            public string EmpId { get; set; }
            public string ComId { get; set; }
            public string dtDate { get; set; }
            public string InTime { get; set; }
            public string OutTime { get; set; }
            public string AttStatus { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesByCompany(Guid comId)
        {
            var employees = await _unitOfWork.Employees
                .GetAllAsync(e => e.ComId == comId);

            var result = employees.Select(e => new {
                empId = e.EmpId,
                empName = e.EmpName
            });

            return Json(result);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(Guid bulkComId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please upload a valid Excel file." });

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var attendances = new List<Attendance>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // ধরে নিচ্ছি 1st row header
                    {
                        var empId = worksheet.Cells[row, 1].Text;
                        var date = worksheet.Cells[row, 2].Text;
                        var inTime = worksheet.Cells[row, 3].Text;
                        var outTime = worksheet.Cells[row, 4].Text;
                        var status = worksheet.Cells[row, 5].Text;

                        if (string.IsNullOrEmpty(empId)) continue;
                        var emp = await _unitOfWork.Employees.GetAsync(Guid.Parse(empId));
                        var com = await _unitOfWork.Companies.GetAsync(bulkComId);

                        attendances.Add(new Attendance
                        {
                            Id = Guid.NewGuid(),
                            ComId = bulkComId,
                            EmpId = emp.EmpId,
                            dtDate = DateOnly.Parse(date),
                            InTime = TimeOnly.Parse(inTime),
                            OutTime = TimeOnly.Parse(outTime),
                            AttStatus = status,
                            Employee = emp,
                            Company = com
                        });

                    }
                }
            }

            if (attendances.Any())
            {
                await _unitOfWork.Attendances.AddRangeAsync(attendances);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = $"{attendances.Count} records uploaded successfully." });
            }

            return Json(new { success = false, message = "No records found in Excel." });
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> DownloadAttendanceTemplate(Guid comId)
        {
            if (comId == Guid.Empty)
                return BadRequest("Company is required.");

            var employees = await _unitOfWork.Employees
                .GetQueryable()
                .Where(e => e.ComId == comId)
                .ToListAsync();

            if (!employees.Any())
                return BadRequest("No employees found for this company.");

           
            ExcelPackage.License.SetLicense(LicenseContext.NonCommercial);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Template");

            // Header
            ws.Cells[1, 1].Value = "EmpId";
            ws.Cells[1, 2].Value = "Date";
            ws.Cells[1, 3].Value = "InTime";
            ws.Cells[1, 4].Value = "OutTime";
            ws.Cells[1, 5].Value = "Status";

            int row = 2;
            foreach (var emp in employees)
            {
                ws.Cells[row, 1].Value = emp.EmpId.ToString();  // GUID
                ws.Cells[row, 2].Value = DateTime.Now.ToString("yyyy-MM-dd"); // Default today
                ws.Cells[row, 3].Value = "09:00"; // Default in time
                ws.Cells[row, 4].Value = "17:00"; // Default out time
                ws.Cells[row, 5].Value = "P";     // Default Present
                row++;
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AttendanceTemplate.xlsx");
        }

    }
}
