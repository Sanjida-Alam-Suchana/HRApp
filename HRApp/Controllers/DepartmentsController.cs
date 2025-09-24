using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                    deptId = department.DeptId.ToString(),
                    department.DeptName,
                    department.ComId,
                    comName = company.ComName
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
                    deptId = existing.DeptId.ToString(),
                    existing.DeptName,
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
            try
            {
                Console.WriteLine($"Fetching departments for comId: {comId} at {DateTime.Now}");
                var departments = _unitOfWork.Departments.GetAll().Include(d => d.Company);
                var filtered = await departments
                    .Where(d => d.ComId == comId)
                    .Select(d => new
                    {
                        deptId = d.DeptId.ToString(),
                        d.DeptName,
                        d.ComId,
                        comName = d.Company != null ? d.Company.ComName : "N/A"
                    })
                    .ToListAsync();

                if (!filtered.Any())
                    Console.WriteLine("No departments found for comId: " + comId);

                return Json(filtered);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartmentsByCompany at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // GET: Fetch all departments (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            try
            {
                Console.WriteLine($"Fetching all departments at {DateTime.Now}");
                var departments = _unitOfWork.Departments.GetAll().Include(d => d.Company);
                var result = await departments
                    .Select(d => new
                    {
                        deptId = d.DeptId.ToString(),
                        d.DeptName,
                        d.ComId,
                        comName = d.Company != null ? d.Company.ComName : "N/A"
                    })
                    .ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No departments found.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllDepartments at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // GET: Fetch single department (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDepartment(Guid id)
        {
            try
            {
                Console.WriteLine($"Fetching department for id: {id} at {DateTime.Now}");
                var department = await _unitOfWork.Departments.GetAsync(id);
                if (department == null)
                {
                    Console.WriteLine($"Department not found for id: {id}");
                    return NotFound(new { success = false, message = "Department not found." });
                }

                var company = await _unitOfWork.Companies.GetAsync(department.ComId);
                return Json(new
                {
                    success = true,
                    department = new
                    {
                        deptId = department.DeptId.ToString(),
                        department.DeptName,
                        department.ComId,
                        comName = company != null ? company.ComName : "N/A"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartment at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }
    }
}