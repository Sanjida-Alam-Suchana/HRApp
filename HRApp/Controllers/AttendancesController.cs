using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Data;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Controllers
{
    public class AttendancesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public AttendancesController(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSummaryGenerate(Guid? comId, string summaryMonth)
        {
            if (comId is null) return BadRequest("Company not selected.");
            if (string.IsNullOrEmpty(summaryMonth)) return BadRequest("Month not selected.");

            var date = DateTime.ParseExact(summaryMonth, "yyyy-MM", null);
            int year = date.Year;
            int month = date.Month;

            await _unitOfWork.ExecRawAsync("CALL CalculateAttendanceSummary({0}, {1}, {2})", comId.Value, year, month);
            return Ok(new { ok = true });
        }

        // POST: Salaries/Calculate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(Guid comId, int dtYear, int dtMonth)
        {
            if (comId == Guid.Empty || dtYear <= 0 || dtMonth < 1 || dtMonth > 12)
            {
                return Json(new { success = false, message = "Invalid parameters." });
            }

            try
            {
                await _unitOfWork.ExecRawAsync("CALL \"CalculateSalary\"({0}, {1}, {2})", comId, dtYear, dtMonth);
                var salaries = await _unitOfWork.Salaries
                    .GetQueryable()
                    .Where(s => s.ComId == comId && s.dtYear == dtYear && s.dtMonth == dtMonth)
                    .Include(s => s.Employee)
                    .ToListAsync();
                return Json(new { success = true, message = "Salary calculated successfully!", data = salaries });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> AttendanceIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync(); // Ensure this is set

            var selectedComId = Request.Cookies["SelectedComId"];

            var query = _context.Attendances
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
            if (attendance.EmpId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select an employee." });
            }

            if (attendance.ComId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select a company." });
            }

            if (attendance.InTime == default || attendance.OutTime == default)
            {
                return Json(new { success = false, message = "In time and out time are required." });
            }

            var employee = await _unitOfWork.Employees.GetAsync(attendance.EmpId);
            if (employee != null && employee.Shift != null)
            {
                var startTime = employee.Shift.StartTime;
                attendance.AttStatus = attendance.InTime > startTime ? "L" : "P";
            }
            else
            {
                attendance.AttStatus = "P";
            }

            attendance.Id = Guid.NewGuid();
            await _unitOfWork.Attendances.AddAsync(attendance);
            try
            {
                await _unitOfWork.SaveAsync();
                System.Diagnostics.Debug.WriteLine($"Attendance created with ID: {attendance.Id}, ComId: {attendance.ComId}"); // Log for debug
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}"); // Log error
                return Json(new { success = false, message = $"Failed to save attendance: {ex.Message}" });
            }
            return Json(new { success = true, message = "Attendance created!", id = attendance.Id.ToString(), attStatus = attendance.AttStatus });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceEdit(Guid id, Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return Json(new { success = false, message = "Invalid attendance ID." });
            }

            if (string.IsNullOrEmpty(attendance.AttStatus))
            {
                return Json(new { success = false, message = "Attendance status is required." });
            }

            if (attendance.EmpId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select an employee." });
            }

            if (attendance.ComId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select a company." });
            }

            if (attendance.InTime == default || attendance.OutTime == default)
            {
                return Json(new { success = false, message = "In time and out time are required." });
            }

            var employee = await _unitOfWork.Employees.GetAsync(attendance.EmpId);
            if (employee != null && employee.Shift != null)
            {
                var startTime = employee.Shift.StartTime;
                attendance.AttStatus = attendance.InTime > startTime ? "L" : "P";
            }

            var existingAttendance = await _unitOfWork.Attendances.GetAsync(id);
            if (existingAttendance == null)
            {
                return Json(new { success = false, message = "Attendance not found." });
            }

            existingAttendance.dtDate = attendance.dtDate;
            existingAttendance.AttStatus = attendance.AttStatus;
            existingAttendance.InTime = attendance.InTime;
            existingAttendance.OutTime = attendance.OutTime;
            existingAttendance.EmpId = attendance.EmpId;
            existingAttendance.ComId = attendance.ComId;

            try
            {
                await _unitOfWork.SaveAsync();
                System.Diagnostics.Debug.WriteLine($"Attendance updated with ID: {id}, ComId: {attendance.ComId}"); // Log for debug
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}"); // Log error
                return Json(new { success = false, message = $"Failed to update attendance: {ex.Message}" });
            }
            return Json(new { success = true, message = "Attendance updated!", attStatus = attendance.AttStatus });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var attendance = await _unitOfWork.Attendances.GetAsync(id);
            if (attendance == null)
            {
                return Json(new { success = false, message = "Attendance not found." });
            }

            await _unitOfWork.Attendances.DeleteAsync(id);
            try
            {
                await _unitOfWork.SaveAsync();
                System.Diagnostics.Debug.WriteLine($"Attendance deleted with ID: {id}"); // Log for debug
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}"); // Log error
                return Json(new { success = false, message = $"Failed to delete attendance: {ex.Message}" });
            }
            return Json(new { success = true, message = "Attendance deleted!" });
        }
    }
}