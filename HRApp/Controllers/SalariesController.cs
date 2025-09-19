using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

        // GET: Salaries/SalaryIndex
        // SalaryIndex Action
        public async Task<IActionResult> SalaryIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            var salaries = await _unitOfWork.Salaries.GetQueryable()
                .Include(s => s.Employee) // Ensure Employee is included
                .Include(s => s.Company)  // Ensure Company is included
                .ToListAsync();

            if (!string.IsNullOrEmpty(selectedComId))
            {
                salaries = salaries
                    .Where(s => s.ComId != Guid.Empty && s.ComId.ToString() == selectedComId)
                    .ToList();
            }

            return View(salaries);
        }

        // GET: Salaries/Calculate
        public async Task<IActionResult> Calculate()
        {
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            return View();
        }

        // POST: Salaries/Calculate
        // Calculate POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(Guid comId, int dtYear, int dtMonth)
        {
            try
            {
                await _unitOfWork.ExecRawAsync("CALL \"CalculateSalary\"({0}, {1}, {2})", comId, dtYear, dtMonth);
                return Json(new { success = true, message = "Salary generated!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Salaries/SalaryCreate
        public async Task<IActionResult> SalaryCreate()
        {
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync(); // Ensure this is populated
            return View();
        }

        // POST: Salaries/SalaryCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalaryCreate(Salary salary)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync();
                return View(salary);
            }

            salary.SalaryId = Guid.NewGuid();
            await _unitOfWork.Salaries.AddAsync(salary);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Salary created successfully!" });
        }

        // GET: Salaries/SalaryEdit/{id}
        public async Task<IActionResult> SalaryEdit(Guid id)
        {
            var salary = await _unitOfWork.Salaries.GetAsync(id);
            if (salary == null) return NotFound();
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Employees = await _unitOfWork.Employees.GetAllAsync(); // Ensure this is populated
            return View(salary);
        }

        // POST: Salaries/SalaryEdit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalaryEdit(Guid id, Salary salary)
        {
            if (id != salary.SalaryId)
                return Json(new { success = false, message = "Invalid salary ID." });

            var existingSalary = await _unitOfWork.Salaries.GetAsync(id);
            if (existingSalary == null)
                return Json(new { success = false, message = "Salary not found." });

            existingSalary.EmpId = salary.EmpId;
            existingSalary.ComId = salary.ComId;
            existingSalary.dtYear = salary.dtYear;
            existingSalary.dtMonth = salary.dtMonth;
            existingSalary.Basic = salary.Basic;
            existingSalary.HRent = salary.HRent;
            existingSalary.Medical = salary.Medical;
            existingSalary.Gross = salary.Gross;
            existingSalary.AbsentDays = salary.AbsentDays;
            existingSalary.IsPaid = salary.IsPaid;
            existingSalary.PaidAmount = salary.PaidAmount;

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Salary updated successfully!" });
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
        // POST: Salaries/Pay/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(Guid id)
        {
            var salary = await _unitOfWork.Salaries.GetAsync(id);
            if (salary == null)
                return Json(new { success = false, message = "Salary not found." });

            if (salary.IsPaid)
                return Json(new { success = false, message = "Salary is already paid." });

           
            salary.IsPaid = true;
            salary.PaidAmount = salary.PayableAmount;

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Salary paid successfully!" });
        }

    }
}