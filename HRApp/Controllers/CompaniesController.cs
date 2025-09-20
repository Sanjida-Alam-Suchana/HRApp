using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HRApp.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompaniesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Companies
        public async Task<IActionResult> CompanyIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            return View(companies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyCreate([FromForm] Company company)
        {
            Console.WriteLine($"Received Company: ComName={company.ComName}, Basic={company.Basic}, HRent={company.HRent}, Medical={company.Medical}, IsInactive={company.IsInactive}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("ModelState Errors: " + string.Join(", ", errors));
                return Json(new { success = false, message = "Invalid input data: " + string.Join(", ", errors) });
            }

            if (string.IsNullOrWhiteSpace(company.ComName))
                return Json(new { success = false, message = "Company name is required." });

            if (company.Basic < 0 || company.HRent < 0 || company.Medical < 0)
                return Json(new { success = false, message = "Numeric values cannot be negative." });

            company.ComId = Guid.NewGuid();
            await _unitOfWork.Companies.AddAsync(company);
            await _unitOfWork.SaveAsync();

            Console.WriteLine($"Company Created: ID={company.ComId}, Name={company.ComName}, Basic={company.Basic}, HRent={company.HRent}, Medical={company.Medical}, IsInactive={company.IsInactive}");

            var response = new
            {
                success = true,
                message = "Company created!",
                company = new
                {
                    ComId = company.ComId,
                    ComName = company.ComName,
                    Basic = company.Basic,
                    HRent = company.HRent,
                    Medical = company.Medical,
                    IsInactive = company.IsInactive
                }
            };

            // Preserve PascalCase in JSON (fix for case sensitivity issue)
            var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
            Console.WriteLine("Response Sent: " + JsonSerializer.Serialize(response, options));
            return Json(response, options);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyEdit(Guid id, [FromForm] Company company)
        {
            Console.WriteLine($"Edit Received: ID={id}, ComName={company.ComName}, Basic={company.Basic}, HRent={company.HRent}, Medical={company.Medical}, IsInactive={company.IsInactive}");

            var existingCompany = await _unitOfWork.Companies.GetAsync(id);
            if (existingCompany == null)
                return Json(new { success = false, message = "Company not found." });

            if (string.IsNullOrWhiteSpace(company.ComName))
                return Json(new { success = false, message = "Company name is required." });

            existingCompany.ComName = company.ComName;
            existingCompany.Basic = company.Basic;
            existingCompany.HRent = company.HRent;
            existingCompany.Medical = company.Medical;
            existingCompany.IsInactive = company.IsInactive;

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Company updated!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var company = await _unitOfWork.Companies.GetAsync(id);
            if (company == null)
                return Json(new { success = false, message = "Company not found." });

            await _unitOfWork.Companies.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Company deleted!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            return Json(companies.Select(c => new { c.ComId, c.ComName }));
        }
    }
}