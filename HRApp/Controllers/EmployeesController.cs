using Microsoft.AspNetCore.Mvc;
using HRApp.Models;
using HRApp.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 

namespace HRApp.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public EmployeesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Employees/EmployeeIndex
        public async Task<IActionResult> EmployeeIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            var query = _unitOfWork.Employees.GetQueryable() // Use GetQueryable if available
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Designation);
            var allEmployees = await query.ToListAsync(); // Materialize after Include
            var employees = string.IsNullOrEmpty(selectedComId)
                ? allEmployees
                : allEmployees.Where(e => e.ComId.ToString() == selectedComId).ToList();

            return View(employees);
        }

        // POST: Employees/EmployeeCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.EmpCode) || string.IsNullOrEmpty(employee.EmpName) || employee.ComId == Guid.Empty || employee.ShiftId == Guid.Empty || employee.DeptId == Guid.Empty || employee.DesigId == Guid.Empty || employee.Gross <= 0 || employee.DtJoin == default)
            {
                return Json(new { success = false, message = "All fields are required, and Gross must be greater than 0." });
            }

            var existing = await _unitOfWork.Employees.GetAllAsync();
            if (existing.Any(e => e.EmpCode == employee.EmpCode))
            {
                return Json(new { success = false, message = "Employee code must be unique." });
            }

            // Validate related entities
            var company = await _unitOfWork.Companies.GetAsync(employee.ComId);
            if (company == null)
            {
                return Json(new { success = false, message = "Selected company does not exist." });
            }
            if (await _unitOfWork.Shifts.GetAsync(employee.ShiftId) == null)
            {
                return Json(new { success = false, message = "Selected shift does not exist." });
            }
            if (await _unitOfWork.Departments.GetAsync(employee.DeptId) == null)
            {
                return Json(new { success = false, message = "Selected department does not exist." });
            }
            if (await _unitOfWork.Designations.GetAsync(employee.DesigId) == null)
            {
                return Json(new { success = false, message = "Selected designation does not exist." });
            }

            // Calculate salary components (assuming non-nullable decimal properties in Company)
            employee.Basic = employee.Gross * (company.Basic > 0 ? company.Basic : 0.5m);
            employee.HRent = employee.Gross * (company.HRent > 0 ? company.HRent : 0.3m);
            employee.Medical = employee.Gross * (company.Medical > 0 ? company.Medical : 0.15m);
            employee.Others = employee.Gross * 0.05m;

            employee.EmpId = Guid.NewGuid();
            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Employee created successfully!" });
        }

        // GET: Employees/EmployeeEdit/{id}
        public async Task<IActionResult> EmployeeEdit(Guid id)
        {
            var employee = await _unitOfWork.Employees.GetAsync(id);
            if (employee == null) return NotFound();

            // Load related entities
            employee.Company = await _unitOfWork.Companies.GetAsync(employee.ComId);
            employee.Shift = await _unitOfWork.Shifts.GetAsync(employee.ShiftId);
            employee.Department = await _unitOfWork.Departments.GetAsync(employee.DeptId);
            employee.Designation = await _unitOfWork.Designations.GetAsync(employee.DesigId);

            return View(employee); // Note: This is for a separate view; adjust if using modal in Index
        }

        // POST: Employees/EmployeeEdit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeEdit(Guid id, Employee employee)
        {
            if (id != employee.EmpId)
            {
                return Json(new { success = false, message = "Invalid employee ID." });
            }

            if (string.IsNullOrEmpty(employee.EmpCode) || string.IsNullOrEmpty(employee.EmpName) || employee.ComId == Guid.Empty || employee.ShiftId == Guid.Empty || employee.DeptId == Guid.Empty || employee.DesigId == Guid.Empty || employee.Gross <= 0 || employee.DtJoin == default)
            {
                return Json(new { success = false, message = "All fields are required, and Gross must be greater than 0." });
            }

            var existing = await _unitOfWork.Employees.GetAllAsync();
            if (existing.Any(e => e.EmpCode == employee.EmpCode && e.EmpId != id))
            {
                return Json(new { success = false, message = "Employee code must be unique." });
            }

            var existingEmployee = await _unitOfWork.Employees.GetAsync(id);
            if (existingEmployee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            // Validate related entities
            var company = await _unitOfWork.Companies.GetAsync(employee.ComId);
            if (company == null)
            {
                return Json(new { success = false, message = "Selected company does not exist." });
            }
            if (await _unitOfWork.Shifts.GetAsync(employee.ShiftId) == null)
            {
                return Json(new { success = false, message = "Selected shift does not exist." });
            }
            if (await _unitOfWork.Departments.GetAsync(employee.DeptId) == null)
            {
                return Json(new { success = false, message = "Selected department does not exist." });
            }
            if (await _unitOfWork.Designations.GetAsync(employee.DesigId) == null)
            {
                return Json(new { success = false, message = "Selected designation does not exist." });
            }

            // Update fields
            existingEmployee.EmpCode = employee.EmpCode;
            existingEmployee.EmpName = employee.EmpName;
            existingEmployee.ComId = employee.ComId;
            existingEmployee.ShiftId = employee.ShiftId;
            existingEmployee.DeptId = employee.DeptId;
            existingEmployee.DesigId = employee.DesigId;
            existingEmployee.Gender = employee.Gender;
            existingEmployee.Gross = employee.Gross;
            existingEmployee.DtJoin = employee.DtJoin;

            // Recalculate salary components (assuming non-nullable decimal properties in Company)
            existingEmployee.Basic = existingEmployee.Gross * (company.Basic > 0 ? company.Basic : 0.5m);
            existingEmployee.HRent = existingEmployee.Gross * (company.HRent > 0 ? company.HRent : 0.3m);
            existingEmployee.Medical = existingEmployee.Gross * (company.Medical > 0 ? company.Medical : 0.15m);
            existingEmployee.Others = existingEmployee.Gross * 0.05m;

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Employee updated successfully!" });
        }

        // POST: Employees/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var employee = await _unitOfWork.Employees.GetAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            await _unitOfWork.Employees.DeleteAsync(id);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Employee deleted successfully!" });
        }

        // GET: Filter Shifts by Company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetShiftsByCompany(Guid comId)
        {
            var shifts = await _unitOfWork.Shifts.GetAllAsync();
            var filtered = shifts.Where(s => s.ComId == comId)
                .Select(s => new { ShiftId = s.ShiftId.ToString(), ShiftName = s.ShiftName });
            return Json(filtered.ToList());
        }

        // GET: Filter Departments by Company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDeptsByCompany(Guid comId)
        {
            var depts = await _unitOfWork.Departments.GetAllAsync();
            var filtered = depts.Where(d => d.ComId == comId)
                .Select(d => new { DeptId = d.DeptId.ToString(), DeptName = d.DeptName });
            return Json(filtered.ToList());
        }

        // GET: Filter Designations by Company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDesignationsByCompany(Guid comId)
        {
            var designations = await _unitOfWork.Designations.GetAllAsync();
            var filtered = designations.Where(d => d.ComId == comId)
                .Select(d => new { DesigId = d.DesigId.ToString(), DesigName = d.DesigName });
            return Json(filtered.ToList());
        }
    }
}