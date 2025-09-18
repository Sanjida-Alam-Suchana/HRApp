using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Controllers
{
    public class AttendanceSummariesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AttendanceSummariesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        // GET: AttendanceSummaries/AttendanceSummaryGenerate
        public async Task<IActionResult> AttendanceSummaryGenerate()
        {
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync();
            return View();
        }

        // POST: AttendanceSummaries/AttendanceSummaryGenerate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendanceSummaryGenerate(AttendanceSummary summary)
        {
            if (summary.ComId == Guid.Empty)
            {
                return Json(new { success = false, message = "Please select a company." });
            }

            if (summary.SummaryMonth == default)
            {
                return Json(new { success = false, message = "Please select a month and year." });
            }

            // Check duplicate
            var existingSummaries = await _unitOfWork.AttendanceSummaries.GetQueryable()
                .Where(s => s.ComId == summary.ComId
                         && s.SummaryMonth.Year == summary.SummaryMonth.Year
                         && s.SummaryMonth.Month == summary.SummaryMonth.Month)
                .ToListAsync();

            if (existingSummaries.Any())
            {
                return Json(new { success = false, message = "Summary already exists for the selected company and month." });
            }

            var employees = await _unitOfWork.Employees.GetQueryable()
                .Where(e => e.ComId == summary.ComId)
                .ToListAsync();

            var totalDays = DateTime.DaysInMonth(summary.SummaryMonth.Year, summary.SummaryMonth.Month);

            foreach (var employee in employees)
            {
                var newSummary = new AttendanceSummary
                {
                    SummaryId = Guid.NewGuid(),
                    EmpId = employee.EmpId,
                    ComId = summary.ComId,
                    SummaryMonth = DateTime.SpecifyKind(new DateTime(summary.SummaryMonth.Year, summary.SummaryMonth.Month, 1), DateTimeKind.Utc),
                    TotalDays = totalDays,
                    DaysPresent = 20, // TODO: Replace with actual attendance logic
                    DaysAbsent = totalDays - 20, // Placeholder calculation
                    DaysLate = 2,
                    Remarks = "Generated automatically for " + summary.SummaryMonth.ToString("MMMM yyyy")
                };

                await _unitOfWork.AttendanceSummaries.AddAsync(newSummary);
            }

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = $"Attendance summaries generated successfully for {employees.Count} employees!" });
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