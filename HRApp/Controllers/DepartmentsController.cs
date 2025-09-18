using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HRApp.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepartmentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: DepartmentIndex
        public async Task<IActionResult> DepartmentIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var allDepartments = await _unitOfWork.Departments.GetAllAsync();
            return View(allDepartments);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentCreate(Department department)
        {
            if (string.IsNullOrWhiteSpace(department.DeptName) || department.ComId == Guid.Empty)
                return Json(new { success = false, message = "Department name and company are required." });

            // Verify that the company exists
            var company = await _unitOfWork.Companies.GetAsync(department.ComId);
            if (company == null)
                return Json(new { success = false, message = "Selected company does not exist." });

            department.DeptId = Guid.NewGuid();
            await _unitOfWork.Departments.AddAsync(department);
            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Department created successfully!",
                department = new
                {
                    DeptId = department.DeptId.ToString(),
                    department.DeptName,
                    department.ComId,
                    ComName = company.ComName
                }
            });
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentEdit(Guid id, Department department)
        {
            if (id != department.DeptId)
                return Json(new { success = false, message = "Invalid ID." });

            if (string.IsNullOrWhiteSpace(department.DeptName) || department.ComId == Guid.Empty)
                return Json(new { success = false, message = "Department name and company are required." });

            var existing = await _unitOfWork.Departments.GetAsync(id);
            if (existing == null)
                return Json(new { success = false, message = "Department not found." });

            // Verify that the company exists
            var company = await _unitOfWork.Companies.GetAsync(department.ComId);
            if (company == null)
                return Json(new { success = false, message = "Selected company does not exist." });

            existing.DeptName = department.DeptName;
            existing.ComId = department.ComId;

            await _unitOfWork.SaveAsync();

            return Json(new
            {
                success = true,
                message = "Department updated!",
                department = new
                {
                    DeptId = existing.DeptId.ToString(),
                    existing.DeptName,
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
            var department = await _unitOfWork.Departments.GetAsync(id);
            if (department == null)
                return Json(new { success = false, message = "Department not found." });

            await _unitOfWork.Departments.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Department deleted!" });
        }

        // GET: Filter by company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByCompany(Guid comId)
        {
            var allDepartments = await _unitOfWork.Departments.GetAllAsync();
            var filtered = await Task.WhenAll(allDepartments
                .Where(d => d.ComId == comId)
                .Select(async d => new
                {
                    DeptId = d.DeptId.ToString(),
                    d.DeptName,
                    d.ComId,
                    ComName = (await _unitOfWork.Companies.GetAsync(d.ComId))?.ComName ?? "N/A"
                }));
            return Json(filtered.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var allDepartments = await _unitOfWork.Departments.GetAllAsync();
            var result = await Task.WhenAll(allDepartments
                .Select(async d => new
                {
                    DeptId = d.DeptId.ToString(),
                    d.DeptName,
                    d.ComId,
                    ComName = (await _unitOfWork.Companies.GetAsync(d.ComId))?.ComName ?? "N/A"
                }));
            return Json(result.ToList());
        }
    }
}