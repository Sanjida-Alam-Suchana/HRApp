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
        public async Task<IActionResult> AttendanceCreate([FromForm] Attendance attendance)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AttendanceCreate called: EmpId={attendance.EmpId}, ComId={attendance.ComId}, Date={attendance.dtDate}, InTime={attendance.InTime}, OutTime={attendance.OutTime}");

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
                    message = "Attendance created successfully!",
                    attendance = new
                    {
                        id = attendance.Id.ToString(),
                        empName = employee.EmpName,
                        comId = attendance.ComId,
                        date = attendance.dtDate.ToString("yyyy-MM-dd"),
                        inTime = attendance.InTime.ToString("HH:mm"),
                        outTime = attendance.OutTime.ToString("HH:mm"),
                        attStatus = attendance.AttStatus
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AttendanceCreate exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error creating attendance: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceEdit(Guid id, [FromForm] Attendance attendance)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AttendanceEdit called: Id={id}, EmpId={attendance.EmpId}, ComId={attendance.ComId}, Date={attendance.dtDate}, InTime={attendance.InTime}, OutTime={attendance.OutTime}");

                var existing = await _unitOfWork.Attendances.GetAsync(id);
                if (existing == null)
                    return Json(new { success = false, message = "Attendance record not found." });

                var employee = await _unitOfWork.Employees.GetQueryable()
                    .Include(e => e.Shift)
                    .FirstOrDefaultAsync(e => e.EmpId == attendance.EmpId);
                if (employee == null)
                    return Json(new { success = false, message = "Employee not found." });

                var company = await _unitOfWork.Companies.GetAsync(attendance.ComId);
                if (company == null)
                    return Json(new { success = false, message = "Company not found." });

                existing.EmpId = attendance.EmpId;
                existing.ComId = attendance.ComId;
                existing.dtDate = attendance.dtDate;
                existing.InTime = attendance.InTime;
                existing.OutTime = attendance.OutTime;
                existing.Employee = employee;
                existing.Company = company;
                existing.RecalculateStatus(); // Recalculate AttStatus

                await _unitOfWork.Attendances.UpdateAsync(existing);
                await _unitOfWork.SaveAsync();

                return Json(new
                {
                    success = true,
                    message = "Attendance updated successfully!",
                    attendance = new
                    {
                        id = existing.Id.ToString(),
                        empName = employee.EmpName,
                        comId = existing.ComId,
                        date = existing.dtDate.ToString("yyyy-MM-dd"),
                        inTime = existing.InTime.ToString("HH:mm"),
                        outTime = existing.OutTime.ToString("HH:mm"),
                        attStatus = existing.AttStatus
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AttendanceEdit exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error updating attendance: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Delete called with id={id}");
                var attendance = await _unitOfWork.Attendances.GetAsync(id);
                if (attendance == null)
                    return Json(new { success = false, message = "Attendance record not found." });

                await _unitOfWork.Attendances.DeleteAsync(id);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Attendance deleted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error deleting attendance: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesByCompany(Guid comId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetEmployeesByCompany called with comId={comId}");
                var employees = await _unitOfWork.Employees
                    .GetAllAsync(e => e.ComId == comId);

                var result = employees.Select(e => new
                {
                    empId = e.EmpId.ToString(),
                    empName = e.EmpName
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetEmployeesByCompany exception: {ex.Message}");
                return Json(new { success = false, message = $"Error fetching employees: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance(Guid id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetAttendance called with id={id}");
                var att = await _unitOfWork.Attendances.GetQueryable()
                    .Include(a => a.Employee)
                        .ThenInclude(e => e.Shift)
                    .Include(a => a.Company)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (att == null)
                    return Json(new { success = false, message = "Attendance record not found." });

                return Json(new
                {
                    success = true,
                    attendance = new
                    {
                        id = att.Id.ToString(),
                        comId = att.ComId.ToString(),
                        empId = att.EmpId.ToString(),
                        dtDate = att.dtDate.ToString("yyyy-MM-dd"),
                        inTime = att.InTime.ToString("HH:mm"),
                        outTime = att.OutTime.ToString("HH:mm"),
                        attStatus = att.AttStatus
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAttendance exception: {ex.Message}");
                return Json(new { success = false, message = $"Error fetching attendance: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendancesByCompany(Guid comId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetAttendancesByCompany called with comId={comId}");
                var attendances = await _unitOfWork.Attendances.GetQueryable()
                    .Include(a => a.Employee)
                    .Include(a => a.Company)
                    .Where(a => a.ComId == comId)
                    .Select(a => new
                    {
                        id = a.Id.ToString(),
                        comId = a.ComId.ToString(),
                        empName = a.Employee != null ? a.Employee.EmpName : "Not Found",
                        date = a.dtDate.ToString("yyyy-MM-dd"),
                        inTime = a.InTime.ToString("HH:mm"),
                        outTime = a.OutTime.ToString("HH:mm"),
                        attStatus = a.AttStatus
                    }).ToListAsync();

                return Json(new { success = true, data = attendances });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAttendancesByCompany exception: {ex.Message}");
                return Json(new { success = false, message = $"Error fetching attendances: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAttendances()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetAllAttendances called at {DateTime.Now}");
                var attendances = await _unitOfWork.Attendances.GetQueryable()
                    .Include(a => a.Employee)
                    .Include(a => a.Company)
                    .Select(a => new
                    {
                        id = a.Id.ToString(),
                        comId = a.ComId.ToString(),
                        empName = a.Employee != null ? a.Employee.EmpName : "Not Found",
                        date = a.dtDate.ToString("yyyy-MM-dd"),
                        inTime = a.InTime.ToString("HH:mm"),
                        outTime = a.OutTime.ToString("HH:mm"),
                        attStatus = a.AttStatus
                    }).ToListAsync();

                return Json(new { success = true, data = attendances });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllAttendances exception: {ex.Message}");
                return Json(new { success = false, message = $"Error fetching attendances: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(Guid bulkComId, IFormFile file)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UploadExcel called with bulkComId={bulkComId}, file={file?.FileName}");
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Please upload a valid Excel file." });

                var attendances = new List<Attendance>();

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension?.Rows ?? 0;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var empIdText = worksheet.Cells[row, 1].Text;
                            var dateText = worksheet.Cells[row, 2].Text;
                            var inTimeText = worksheet.Cells[row, 3].Text;
                            var outTimeText = worksheet.Cells[row, 4].Text;

                            if (string.IsNullOrEmpty(empIdText) || !Guid.TryParse(empIdText, out Guid empId))
                                continue;

                            var emp = await _unitOfWork.Employees.GetQueryable()
                                .Include(e => e.Shift)
                                .FirstOrDefaultAsync(e => e.EmpId == empId);
                            if (emp == null)
                                continue;

                            var com = await _unitOfWork.Companies.GetAsync(bulkComId);
                            if (com == null)
                                continue;

                            if (!DateOnly.TryParse(dateText, out var date))
                                continue;

                            if (!TimeOnly.TryParse(inTimeText, out var inTime) || !TimeOnly.TryParse(outTimeText, out var outTime))
                                continue;

                            var newAttendance = new Attendance
                            {
                                Id = Guid.NewGuid(),
                                ComId = bulkComId,
                                EmpId = empId,
                                dtDate = date,
                                InTime = inTime,
                                OutTime = outTime,
                                Employee = emp,
                                Company = com
                            };

                            newAttendance.RecalculateStatus();
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

                return Json(new { success = false, message = "No valid records found in Excel." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UploadExcel exception: {ex.Message}");
                return Json(new { success = false, message = $"Error uploading Excel: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttendanceTemplate(Guid comId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAttendanceTemplate called with comId={comId}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAttendanceTemplate exception: {ex.Message}");
                return BadRequest($"Error generating template: {ex.Message}");
            }
        }
    }
}