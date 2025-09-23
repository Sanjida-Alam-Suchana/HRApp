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

            var query = _unitOfWork.Attendances.GetQueryable()
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Shift)
                .Include(a => a.Company)
                .AsQueryable();

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
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmpId == attendance.EmpId);
            if (employee == null)
                return Json(new { success = false, message = "Employee not found." });

            var company = await _unitOfWork.Companies.GetAsync(attendance.ComId);
            if (company == null)
                return Json(new { success = false, message = "Company not found." });

            attendance.Id = Guid.NewGuid();
            attendance.Employee = employee;
            attendance.Company = company;
            attendance.RecalculateStatus(); // Calculate and set AttStatus

            await _unitOfWork.Attendances.AddAsync(attendance);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Attendance created!",
                attendance = new
                {
                    id = attendance.Id,
                    empName = employee.EmpName,
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
            var existing = await _unitOfWork.Attendances.GetAsync(id);
            if (existing == null) return Json(new { success = false, message = "Attendance not found." });

            var employee = await _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Shift)
                .FirstOrDefaultAsync(e => e.EmpId == attendance.EmpId);
            if (employee == null) return Json(new { success = false, message = "Employee not found." });

            existing.EmpId = attendance.EmpId;
            existing.ComId = employee.ComId;
            existing.dtDate = attendance.dtDate;
            existing.InTime = attendance.InTime;
            existing.OutTime = attendance.OutTime;
            existing.Employee = employee;
            existing.Company = await _unitOfWork.Companies.GetAsync(employee.ComId);
            existing.RecalculateStatus(); // Recalculate AttStatus

            await _unitOfWork.Attendances.UpdateAsync(existing);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Attendance updated!",
                attendance = new
                {
                    id = existing.Id,
                    empName = employee.EmpName,
                    date = existing.dtDate.ToString("yyyy-MM-dd"),
                    inTime = existing.InTime.ToString("HH:mm"),
                    outTime = existing.OutTime.ToString("HH:mm"),
                    attStatus = existing.AttStatus
                }
            });
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

        [HttpGet]
        public async Task<IActionResult> GetAttendance(Guid id)
        {
            var att = await _unitOfWork.Attendances.GetQueryable()
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Shift)
                .Include(a => a.Company)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (att == null) return NotFound();

            return Json(new
            {
                id = att.Id,
                comId = att.ComId,
                empId = att.EmpId,
                dtDate = att.dtDate.ToString("yyyy-MM-dd"),
                inTime = att.InTime.ToString("HH:mm"),
                outTime = att.OutTime.ToString("HH:mm"),
                attStatus = att.AttStatus
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendancesByCompany(Guid comId)
        {
            try
            {
                Console.WriteLine($"Fetching attendances for ComId {comId} at {DateTime.Now}");
                var attendances = _unitOfWork.Attendances.GetQueryable()
                    .Include(a => a.Employee)
                    .Include(a => a.Company)
                    .Where(a => a.ComId == comId);

                var result = await attendances.Select(a => new
                {
                    id = a.Id,
                    comId = a.ComId,
                    empName = a.Employee != null ? a.Employee.EmpName : "Not Found",
                    date = a.dtDate.ToString("yyyy-MM-dd"),
                    inTime = a.InTime.ToString("HH:mm"),
                    outTime = a.OutTime.ToString("HH:mm"),
                    attStatus = a.AttStatus
                }).ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No attendance records found for this company.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAttendancesByCompany: {ex}");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(Guid bulkComId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please upload a valid Excel file." });

            var attendances = new List<Attendance>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var empIdText = worksheet.Cells[row, 1].Text;
                        var date = worksheet.Cells[row, 2].Text;
                        var inTime = worksheet.Cells[row, 3].Text;
                        var outTime = worksheet.Cells[row, 4].Text;
                        var status = worksheet.Cells[row, 5].Text;

                        if (string.IsNullOrEmpty(empIdText)) continue;

                        if (!Guid.TryParse(empIdText, out Guid empId)) continue;

                        var emp = await _unitOfWork.Employees.GetQueryable()
                            .Include(e => e.Shift)
                            .FirstOrDefaultAsync(e => e.EmpId == empId);
                        if (emp == null) continue;

                        var com = await _unitOfWork.Companies.GetAsync(bulkComId);

                        var newAttendance = new Attendance
                        {
                            Id = Guid.NewGuid(),
                            ComId = bulkComId,
                            EmpId = emp.EmpId,
                            dtDate = DateOnly.Parse(date),
                            InTime = TimeOnly.Parse(inTime),
                            OutTime = TimeOnly.Parse(outTime),
                            Employee = emp,
                            Company = com
                        };

                        newAttendance.RecalculateStatus(); // Calculate and set AttStatus

                        attendances.Add(newAttendance);
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

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Template");

            ws.Cells[1, 1].Value = "EmpId";
            ws.Cells[1, 2].Value = "Date";
            ws.Cells[1, 3].Value = "InTime";
            ws.Cells[1, 4].Value = "OutTime";
            ws.Cells[1, 5].Value = "Status";

            int row = 2;
            foreach (var emp in employees)
            {
                ws.Cells[row, 1].Value = emp.EmpId.ToString();
                ws.Cells[row, 2].Value = DateTime.Now.ToString("yyyy-MM-dd");
                ws.Cells[row, 3].Value = "08:00";
                ws.Cells[row, 4].Value = "18:00";
                ws.Cells[row, 5].Value = "P";
                row++;
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AttendanceTemplate.xlsx");
        }
    }
}