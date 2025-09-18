using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> CompanyCreate(Company company)
        {
            if (string.IsNullOrEmpty(company.ComName))
                return Json(new { success = false, message = "Company name is required." });

            company.ComId = Guid.NewGuid();
            await _unitOfWork.Companies.AddAsync(company);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Company created!",
                company = new
                {
                    ComId = company.ComId,
                    ComName = company.ComName,
                    Basic = company.Basic,
                    Hrent = company.Hrent,
                    Medical = company.Medical,
                    IsInactive = company.IsInactive
                }
            });

        }

        // POST: Companies/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyEdit(Guid id, Company company)
        {
            var existingCompany = await _unitOfWork.Companies.GetAsync(id);
            if (existingCompany == null)
                return Json(new { success = false, message = "Company not found." });

            existingCompany.ComName = company.ComName;
            existingCompany.Basic = company.Basic;
            existingCompany.Hrent = company.Hrent;
            existingCompany.Medical = company.Medical;
            existingCompany.IsInactive = company.IsInactive;

            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Company updated!" });
        }

        // POST: Companies/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var company = await _unitOfWork.Companies.GetAsync(id);
            if (company == null) return Json(new { success = false, message = "Company not found." });

            await _unitOfWork.Companies.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Company deleted!" });
        }

// GET: Companies/GetCompanies (for dropdown)

        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            return Json(companies.Select(c => new { c.ComId, c.ComName }));
        }
    }
}

