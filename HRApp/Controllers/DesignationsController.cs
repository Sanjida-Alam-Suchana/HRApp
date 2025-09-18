using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HRApp.Controllers
{
    public class DesignationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DesignationsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: DesignationIndex
        public async Task<IActionResult> DesignationIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var allDesignations = await _unitOfWork.Designations.GetAllAsync();
            return View(allDesignations);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesignationCreate(Designation designation)
        {
            if (string.IsNullOrEmpty(designation.DesigName) || designation.ComId == Guid.Empty)
                return Json(new { success = false, message = "Designation name and company are required." });

            // Verify that the company exists
            var company = await _unitOfWork.Companies.GetAsync(designation.ComId);
            if (company == null)
                return Json(new { success = false, message = "Selected company does not exist." });

            designation.DesigId = Guid.NewGuid();
            await _unitOfWork.Designations.AddAsync(designation);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Designation created successfully!",
                designation = new
                {
                    DesigId = designation.DesigId.ToString(),
                    designation.DesigName,
                    designation.ComId,
                    ComName = company.ComName
                }
            });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DesignationEdit(Guid id, Designation designation)
        {
            if (id != designation.DesigId)
                return Json(new { success = false, message = "Invalid ID." });

            if (string.IsNullOrEmpty(designation.DesigName) || designation.ComId == Guid.Empty)
                return Json(new { success = false, message = "Designation name and company are required." });

            var existing = await _unitOfWork.Designations.GetAsync(id);
            if (existing == null)
                return Json(new { success = false, message = "Designation not found." });

            // Verify that the company exists
            var company = await _unitOfWork.Companies.GetAsync(designation.ComId);
            if (company == null)
                return Json(new { success = false, message = "Selected company does not exist." });

            existing.DesigName = designation.DesigName;
            existing.ComId = designation.ComId;

            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Designation updated!",
                designation = new
                {
                    DesigId = existing.DesigId.ToString(),
                    existing.DesigName,
                    existing.ComId,
                    ComName = company.ComName
                }
            });
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var designation = await _unitOfWork.Designations.GetAsync(id);
            if (designation == null)
                return Json(new { success = false, message = "Designation not found." });

            await _unitOfWork.Designations.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Designation deleted!" });
        }

        // GET: Filter by company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDesignationsByCompany(Guid comId)
        {
            var allDesignations = await _unitOfWork.Designations.GetAllAsync();
            var filtered = await Task.WhenAll(allDesignations
                .Where(d => d.ComId == comId)
                .Select(async d => new
                {
                    DesigId = d.DesigId.ToString(),
                    d.DesigName,
                    d.ComId,
                    ComName = (await _unitOfWork.Companies.GetAsync(d.ComId))?.ComName ?? "N/A"
                }));
            return Json(filtered.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDesignations()
        {
            var allDesignations = await _unitOfWork.Designations.GetAllAsync();
            var result = await Task.WhenAll(allDesignations
                .Select(async d => new
                {
                    DesigId = d.DesigId.ToString(),
                    d.DesigName,
                    d.ComId,
                    ComName = (await _unitOfWork.Companies.GetAsync(d.ComId))?.ComName ?? "N/A"
                }));
            return Json(result.ToList());
        }
    }
}