using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace HRApp.Controllers
{
    public class AttendanceSummariesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AttendanceSummariesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateAttendanceSummary(Guid comId, int dtYear, int dtMonth)
        {
            try
            {
                await _unitOfWork.ExecRawAsync("CALL \"CalculateAttendanceSummary\"({0}, {1}, {2})", comId, dtYear, dtMonth);
                return Json(new { success = true, message = "Attendance summary generated!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CalculateAttendanceSummary: {ex}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: AttendanceSummaries/AttendanceSummaryIndex
        public async Task<IActionResult> AttendanceSummaryIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            var summaries = await _unitOfWork.AttendanceSummaries.GetQueryable()
                .Include(s => s.Employee)
                .Include(s => s.Company)
                .ToListAsync();

            if (!string.IsNullOrEmpty(selectedComId))
            {
                summaries = summaries
                    .Where(s => s.ComId != Guid.Empty && s.ComId.ToString() == selectedComId)
                    .ToList();
            }

            return View(summaries);
        }

        // POST: AttendanceSummaries/AttendanceSummaryGenerate (Consolidated method, handles string inputs, checks duplicates)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSummaryGenerate(string ComId, string SummaryMonth)
        {
            if (!Guid.TryParse(ComId, out var comId) || comId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select a company." });
            }
            if (!DateTime.TryParseExact(SummaryMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var summaryDate))
            {
                return Json(new { success = false, message = "Invalid month format." });
            }

            // Check duplicate
            var existingSummaries = await _unitOfWork.AttendanceSummaries.GetQueryable()
                .Where(s => s.ComId == comId
                         && s.SummaryMonth.Year == summaryDate.Year
                         && s.SummaryMonth.Month == summaryDate.Month)
                .ToListAsync();

            if (existingSummaries.Any())
            {
                return Json(new { success = false, message = "Summary already exists for the selected company and month." });
            }

            try
            {
                await _unitOfWork.ExecRawAsync("CALL \"CalculateAttendanceSummary\"({0}, {1}, {2})", comId, summaryDate.Year, summaryDate.Month);
                return Json(new { success = true, message = $"Attendance summaries generated successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating summary: {ex}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: GetEmployeesByCompany for AJAX
        [HttpGet]
        public async Task<IActionResult> GetEmployeesByCompany(Guid comId)
        {
            var employees = await _unitOfWork.Employees.GetQueryable()
                .Where(e => e.ComId == comId)
                .Select(e => new { EmpId = e.EmpId.ToString(), EmpName = e.EmpName })
                .ToListAsync();

            return Json(employees);
        }

        // GET: AttendanceSummaries/AttendanceSummaryEdit/{id}
        public async Task<IActionResult> AttendanceSummaryEdit(Guid id)
        {
            var summary = await _unitOfWork.AttendanceSummaries.GetAsync(id);
            if (summary == null) return NotFound();

            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync();
            return View(summary);
        }

        // POST: AttendanceSummaries/AttendanceSummaryEdit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSummaryEdit(Guid id, AttendanceSummary summary)
        {
            if (id != summary.SummaryId)
                return Json(new { success = false, message = "Invalid summary ID." });

            var existingSummary = await _unitOfWork.AttendanceSummaries.GetAsync(id);
            if (existingSummary == null)
                return Json(new { success = false, message = "Attendance summary not found." });

            existingSummary.EmpId = summary.EmpId;
            existingSummary.ComId = summary.ComId;
            existingSummary.SummaryMonth = DateTime.SpecifyKind(new DateTime(summary.SummaryMonth.Year, summary.SummaryMonth.Month, 1), DateTimeKind.Utc);
            existingSummary.TotalDays = summary.TotalDays;
            existingSummary.DaysPresent = summary.DaysPresent;
            existingSummary.DaysAbsent = summary.DaysAbsent;
            existingSummary.DaysLate = summary.DaysLate;
            existingSummary.Remarks = summary.Remarks;

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Attendance summary updated successfully!" });
        }

        // POST: AttendanceSummaries/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var summary = await _unitOfWork.AttendanceSummaries.GetAsync(id);
            if (summary == null)
                return Json(new { success = false, message = "Attendance summary not found." });

            await _unitOfWork.AttendanceSummaries.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Attendance summary deleted successfully!" });
        }
    }
}