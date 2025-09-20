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
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync();

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

            return Json(new { success = true, message = "Attendance created!", id = attendance.Id.ToString(), attStatus = attendance.AttStatus });
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

        [HttpPost]
        public async Task<IActionResult> BulkCreate([FromBody] List<BulkAttendanceModel> attendances)
        {
            if (attendances == null || !attendances.Any())
                return Json(new { success = false, message = "No attendance data provided." });

            int addedCount = 0;
            var errors = new List<string>();

            foreach (var item in attendances)
            {
                try
                {
                    var employee = await _unitOfWork.Employees.GetAsync(Guid.Parse(item.EmpId));
                    if (employee == null)
                    {
                        errors.Add($"Employee not found: {item.EmpId}");
                        continue;
                    }

                    var attDate = DateOnly.Parse(item.dtDate);
                    var existing = await _unitOfWork.Attendances.GetQueryable()
                        .FirstOrDefaultAsync(a => a.EmpId == employee.EmpId && a.dtDate == attDate);

                    if (existing != null)
                    {
                        errors.Add($"Attendance already exists for {employee.EmpName} on {attDate}");
                        continue;
                    }

                    var attendance = new Attendance
                    {
                        Id = Guid.NewGuid(),
                        EmpId = employee.EmpId,
                        ComId = Guid.Parse(item.ComId),
                        dtDate = attDate,
                        InTime = TimeOnly.Parse(item.InTime),
                        OutTime = TimeOnly.Parse(item.OutTime),
                        AttStatus = item.AttStatus,
                        Employee = employee,
                        Company = employee.Company
                    };

                    await _unitOfWork.Attendances.AddAsync(attendance);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error for EmpId {item.EmpId}: {ex.Message}");
                }
            }

            try
            {
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = $"Bulk attendance saved! {addedCount} records.", errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error saving records: {ex.Message}" });
            }
        }
        public async Task<IActionResult> DownloadBulkAttendance(Guid comId, DateTime date)
        {
            var attendances = await _unitOfWork.Attendances.GetQueryable()
                .Include(a => a.Employee)
                .Include(a => a.Company)
                .Where(a => a.ComId == comId && a.dtDate == DateOnly.FromDateTime(date))
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Attendance");
            ws.Cells[1, 1].Value = "Employee";
            ws.Cells[1, 2].Value = "Date";
            ws.Cells[1, 3].Value = "In Time";
            ws.Cells[1, 4].Value = "Out Time";
            ws.Cells[1, 5].Value = "Status";

            int row = 2;
            foreach (var att in attendances)
            {
                ws.Cells[row, 1].Value = att.Employee.EmpName;
                ws.Cells[row, 2].Value = att.dtDate.ToString("yyyy-MM-dd");
                ws.Cells[row, 3].Value = att.InTime.ToString("HH:mm");
                ws.Cells[row, 4].Value = att.OutTime.ToString("HH:mm");
                ws.Cells[row, 5].Value = att.AttStatus;
                row++;
            }

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BulkAttendance.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });

            var errors = new List<string>();
            int addedCount = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    string empCode = worksheet.Cells[row, 1].Text?.Trim();
                    string dateStr = worksheet.Cells[row, 2].Text?.Trim();
                    string inTimeStr = worksheet.Cells[row, 3].Text?.Trim();
                    string outTimeStr = worksheet.Cells[row, 4].Text?.Trim();

                    if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(dateStr))
                    {
                        errors.Add($"Row {row}: Missing employee code or date.");
                        continue;
                    }

                    if (!DateTime.TryParse(dateStr, out DateTime date) ||
                        !TimeSpan.TryParse(inTimeStr, out TimeSpan inTime) ||
                        !TimeSpan.TryParse(outTimeStr, out TimeSpan outTime))
                    {
                        errors.Add($"Row {row}: Invalid date/time format.");
                        continue;
                    }

                    var employee = await _unitOfWork.Employees.GetQueryable()
                        .Include(e => e.Company)
                        .Include(e => e.Shift)
                        .FirstOrDefaultAsync(e => e.EmpCode == empCode);

                    if (employee == null)
                    {
                        errors.Add($"Row {row}: Employee not found.");
                        continue;
                    }

                    var existingAttendance = await _unitOfWork.Attendances.GetQueryable()
                        .FirstOrDefaultAsync(a => a.EmpId == employee.EmpId && a.dtDate == DateOnly.FromDateTime(date));

                    if (existingAttendance != null)
                    {
                        errors.Add($"Row {row}: Attendance already exists for {empCode} on {date:yyyy-MM-dd}.");
                        continue;
                    }

                    var attendance = new Attendance
                    {
                        Id = Guid.NewGuid(),
                        EmpId = employee.EmpId,
                        ComId = employee.ComId,
                        dtDate = DateOnly.FromDateTime(date),
                        InTime = TimeOnly.FromTimeSpan(inTime),
                        OutTime = TimeOnly.FromTimeSpan(outTime),
                        AttStatus = (employee.Shift != null && TimeOnly.FromTimeSpan(inTime) > employee.Shift.StartTime) ? "L" : "P",
                        Employee = employee,
                        Company = employee.Company
                    };

                    await _unitOfWork.Attendances.AddAsync(attendance);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row}: {ex.Message}");
                }
            }

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = $"Excel upload completed! {addedCount} records added.", errors });
        }
    }
}
