using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                    desigId = designation.DesigId.ToString(),
                    designation.DesigName,
                    designation.ComId,
                    comName = company.ComName
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
                    desigId = existing.DesigId.ToString(),
                    existing.DesigName,
                    existing.ComId,
                    comName = company.ComName
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
            try
            {
                Console.WriteLine($"Fetching designations for comId: {comId} at {DateTime.Now}");
                var designations = _unitOfWork.Designations.GetAll();
                var filtered = await designations
                    .Where(d => d.ComId == comId)
                    .Select(d => new
                    {
                        desigId = d.DesigId.ToString(),
                        d.DesigName,
                        d.ComId,
                        comName = d.Company != null ? d.Company.ComName : "N/A"
                    })
                    .ToListAsync();

                if (!filtered.Any())
                    Console.WriteLine("No designations found for comId: " + comId);

                return Json(filtered);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDesignationsByCompany at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // GET: Fetch all designations (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAllDesignations()
        {
            try
            {
                Console.WriteLine($"Fetching all designations at {DateTime.Now}");
                var designations = _unitOfWork.Designations.GetAll();
                var result = await designations
                    .Select(d => new
                    {
                        desigId = d.DesigId.ToString(),
                        d.DesigName,
                        d.ComId,
                        comName = d.Company != null ? d.Company.ComName : "N/A"
                    })
                    .ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No designations found.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllDesignations at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // GET: Fetch single designation (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDesignation(Guid id)
        {
            try
            {
                Console.WriteLine($"Fetching designation for id: {id} at {DateTime.Now}");
                var designation = await _unitOfWork.Designations.GetAsync(id);
                if (designation == null)
                {
                    Console.WriteLine($"Designation not found for id: {id}");
                    return NotFound(new { success = false, message = "Designation not found." });
                }

                var company = await _unitOfWork.Companies.GetAsync(designation.ComId);
                return Json(new
                {
                    success = true,
                    designation = new
                    {
                        desigId = designation.DesigId.ToString(),
                        designation.DesigName,
                        designation.ComId,
                        comName = company != null ? company.ComName : "N/A"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDesignation at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }
    }
}
