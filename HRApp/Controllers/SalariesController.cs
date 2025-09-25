using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HRApp.Controllers
{
    public class SalariesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public SalariesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Salaries/Index (with filters)
        public async Task<IActionResult> Index(Guid? comId, int? year, int? month)
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            // Fallback to cookie if no comId param
            if (!comId.HasValue)
            {
                var selectedComIdStr = Request.Cookies["SelectedComId"];
                if (Guid.TryParse(selectedComIdStr, out var cookieComId))
                {
                    comId = cookieComId;
                }
            }
            ViewBag.SelectedComId = comId;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;

            var salaries = await _unitOfWork.Salaries.GetQueryable()
                .Include(s => s.Employee)
                .Include(s => s.Company)
                .Where(s => (!comId.HasValue || s.ComId == comId.Value) &&
                            (!year.HasValue || s.dtYear == year.Value) &&
                            (!month.HasValue || s.dtMonth == month.Value))
                .ToListAsync();

            return View(salaries);
        }
        [HttpGet]
        public async Task<IActionResult> GetSalariesByCompany(Guid comId, int year, int month)
        {
            try
            {
                Console.WriteLine($"Fetching salaries for ComId={comId}, Year={year}, Month={month} at {DateTime.Now}");
                var salaries = _unitOfWork.Salaries.GetQueryable()
                    .Include(s => s.Employee)
                    .Include(s => s.Company)
                    .Where(s => s.ComId == comId && s.dtYear == year && s.dtMonth == month);

                var result = await salaries.Select(s => new
                {
                    salaryId = s.SalaryId,
                    comId = s.ComId,
                    empName = s.Employee != null ? s.Employee.EmpName : "Not Found",
                    salaryMonth = s.SalaryMonth,
                    gross = s.Gross,
                    basic = s.Basic,
                    hRent = s.HRent,
                    medical = s.Medical,
                    absentDays = s.AbsentDays,
                    absentAmount = s.AbsentAmount,
                    payableAmount = s.PayableAmount,
                    paidAmount = s.PaidAmount,
                    isPaid = s.IsPaid
                }).ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No salary records found for this company, year, and month.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSalariesByCompany: {ex}");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // GET: Salaries/GetAllSalaries
        [HttpGet]
        public async Task<IActionResult> GetAllSalaries(int year, int month)
        {
            try
            {
                Console.WriteLine($"Fetching all salaries for Year={year}, Month={month} at {DateTime.Now}");
                var salaries = _unitOfWork.Salaries.GetQueryable()
                    .Include(s => s.Employee)
                    .Include(s => s.Company)
                    .Where(s => s.dtYear == year && s.dtMonth == month);

                var result = await salaries.Select(s => new
                {
                    salaryId = s.SalaryId,
                    comId = s.ComId,
                    empName = s.Employee != null ? s.Employee.EmpName : "Not Found",
                    salaryMonth = s.SalaryMonth,
                    gross = s.Gross,
                    basic = s.Basic,
                    hRent = s.HRent,
                    medical = s.Medical,
                    absentDays = s.AbsentDays,
                    absentAmount = s.AbsentAmount,
                    payableAmount = s.PayableAmount,
                    paidAmount = s.PaidAmount,
                    isPaid = s.IsPaid
                }).ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No salary records found for this year and month.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllSalaries: {ex}");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        // POST: Salaries/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(Guid comId, int year, int month)
        {
            try
            {
                Console.WriteLine($"Generate called: ComId={comId}, Year={year}, Month={month}");

                if (comId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Please select a company." });
                }

                // Check employees
                var employeeCount = await _unitOfWork.Employees.GetQueryable()
                    .CountAsync(e => e.ComId == comId && e.Basic > 0);
                Console.WriteLine($"Valid employees (with Basic > 0): {employeeCount}");

                if (employeeCount == 0)
                {
                    return Json(new { success = false, message = "No employees with valid salary data found for the selected company." });
                }

                // Check attendance summary
                var summaryCount = await _unitOfWork.AttendanceSummaries.GetQueryable()
                    .CountAsync(a => a.ComId == comId &&
                                   a.SummaryMonth.Year == year &&
                                   a.SummaryMonth.Month == month);
                Console.WriteLine($"Attendance summaries: {summaryCount}");

                if (summaryCount == 0)
                {
                    return Json(new { success = false, message = "Please generate Attendance Summary first for this company and month." });
                }

                // Check existing salaries
                var existingCount = await _unitOfWork.Salaries.GetQueryable()
                    .CountAsync(s => s.ComId == comId && s.dtYear == year && s.dtMonth == month);

                if (existingCount > 0)
                {
                    return Json(new { success = false, message = $"Salaries already exist ({existingCount} records). Delete them first if you want to regenerate." });
                }

                // Call procedure
                Console.WriteLine("Calling CalculateSalary procedure...");
                await _unitOfWork.ExecRawAsync("CALL \"CalculateSalary\"(@p0, @p1, @p2)", comId, year, month);
                Console.WriteLine("Procedure call completed");

                // Verify results
                var insertedCount = await _unitOfWork.Salaries.GetQueryable()
                    .CountAsync(s => s.ComId == comId && s.dtYear == year && s.dtMonth == month);
                Console.WriteLine($"Salaries after procedure: {insertedCount}");

                if (insertedCount == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Procedure completed but no records were inserted. Check database logs for details."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Salaries generated successfully! {insertedCount} records created."
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generate error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Salaries/TogglePayment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePayment(Guid id)
        {
            var salary = await _unitOfWork.Salaries.GetAsync(id);
            if (salary == null)
                return Json(new { success = false, message = "Salary not found." });

            salary.IsPaid = !salary.IsPaid;
            salary.PaidAmount = salary.IsPaid ? salary.PayableAmount : 0;  // Toggle payment amount

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = salary.IsPaid ? "Salary marked as paid!" : "Salary marked as unpaid!" });
        }

        // POST: Salaries/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var salary = await _unitOfWork.Salaries.GetAsync(id);
            if (salary == null)
                return Json(new { success = false, message = "Salary not found." });

            await _unitOfWork.Salaries.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Salary deleted successfully!" });
        }
    }
}