using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO; // For Path and File handling

namespace HRApp.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _environment;

        public EmployeesController(IUnitOfWork unitOfWork, IWebHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _environment = environment;
        }

        public async Task<IActionResult> EmployeeIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            var query = _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Designation);
            var allEmployees = await query.ToListAsync();
            Console.WriteLine($"Total employees: {allEmployees.Count}");
            var employees = string.IsNullOrEmpty(selectedComId)
                ? allEmployees
                : allEmployees.Where(e => e.ComId.ToString() == selectedComId).ToList();
            Console.WriteLine($"Filtered employees: {employees.Count}, SelectedComId: {selectedComId}");
            return View(employees);
        }
        // POST: Employees/EmployeeCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(Employee employee, IFormFile? EmployeeImageFile)
        {
            try
            {
                Console.WriteLine($"Received Employee: EmpCode={employee.EmpCode}, EmpName={employee.EmpName}, ComId={employee.ComId}, ShiftId={employee.ShiftId}, DeptId={employee.DeptId}, DesigId={employee.DesigId}, Gross={employee.Gross}, DtJoin={employee.DtJoin}, HasImage={EmployeeImageFile != null}");

                // Basic validation
                if (string.IsNullOrEmpty(employee.EmpCode) ||
                    string.IsNullOrEmpty(employee.EmpName) ||
                    employee.ComId == Guid.Empty ||
                    employee.Gross <= 0 ||
                    employee.DtJoin == default)
                {
                    return Json(new { success = false, message = "Required fields are missing." });
                }

                // Related entity validation
                var company = await _unitOfWork.Companies.GetAsync(employee.ComId);
                if (company == null)
                    return Json(new { success = false, message = "Invalid company." });

                if (employee.ShiftId != Guid.Empty && await _unitOfWork.Shifts.GetAsync(employee.ShiftId) == null)
                    return Json(new { success = false, message = "Invalid shift." });

                if (employee.DeptId != Guid.Empty && await _unitOfWork.Departments.GetAsync(employee.DeptId) == null)
                    return Json(new { success = false, message = "Invalid department." });

                if (employee.DesigId != Guid.Empty && await _unitOfWork.Designations.GetAsync(employee.DesigId) == null)
                    return Json(new { success = false, message = "Invalid designation." });

                if (!Enum.IsDefined(typeof(GenderType), employee.Gender))
                    return Json(new { success = false, message = "Invalid gender." });

                // Salary components
                employee.Basic = employee.Gross * (company.Basic > 0 ? company.Basic : 0.5m);
                employee.HRent = employee.Gross * (company.HRent > 0 ? company.HRent : 0.3m);
                employee.Medical = employee.Gross * (company.Medical > 0 ? company.Medical : 0.15m);
                employee.Others = employee.Gross * 0.05m;

                // Image upload
                if (EmployeeImageFile != null && EmployeeImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images/employees");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + EmployeeImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await EmployeeImageFile.CopyToAsync(fileStream);
                    }

                    employee.EmployeeImage = "/images/employees/" + uniqueFileName;
                }

                employee.EmpId = Guid.NewGuid();
                await _unitOfWork.Employees.AddAsync(employee);
                await _unitOfWork.SaveAsync();

                // Include the created employee in the response
                var createdEmployee = await _unitOfWork.Employees.GetAsync(employee.EmpId);
                createdEmployee.Company = await _unitOfWork.Companies.GetAsync(employee.ComId);
                createdEmployee.Shift = await _unitOfWork.Shifts.GetAsync(employee.ShiftId);
                createdEmployee.Department = await _unitOfWork.Departments.GetAsync(employee.DeptId);
                createdEmployee.Designation = await _unitOfWork.Designations.GetAsync(employee.DesigId);

                return Json(new
                {
                    success = true,
                    message = "Employee created successfully!",
                    employee = new
                    {
                        EmpId = createdEmployee.EmpId,
                        EmpCode = createdEmployee.EmpCode,
                        EmpName = createdEmployee.EmpName,
                        ComId = createdEmployee.ComId,
                        ShiftId = createdEmployee.ShiftId,
                        DeptId = createdEmployee.DeptId,
                        DesigId = createdEmployee.DesigId,
                        Gender = createdEmployee.Gender,
                        Gross = createdEmployee.Gross,
                        Basic = createdEmployee.Basic,
                        HRent = createdEmployee.HRent,
                        Medical = createdEmployee.Medical,
                        Others = createdEmployee.Others,
                        DtJoin = createdEmployee.DtJoin,
                        EmployeeImage = createdEmployee.EmployeeImage,
                        Company = new { ComName = createdEmployee.Company?.ComName },
                        Shift = new { ShiftName = createdEmployee.Shift?.ShiftName },
                        Department = new { DeptName = createdEmployee.Department?.DeptName },
                        Designation = new { DesigName = createdEmployee.Designation?.DesigName }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EmployeeCreate: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Failed to create employee: {ex.Message}" });
            }
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
        public async Task<IActionResult> EmployeeEdit(Guid id, Employee employee, IFormFile? EmployeeImageFile)
        {
            try
            {
                Console.WriteLine($"Received Employee for Edit: EmpId={id}, EmpCode={employee.EmpCode}, EmpName={employee.EmpName}, ComId={employee.ComId}, ShiftId={employee.ShiftId}, DeptId={employee.DeptId}, DesigId={employee.DesigId}, Gross={employee.Gross}, DtJoin={employee.DtJoin}, HasImage={EmployeeImageFile != null}");

                // Fetch the existing employee
                var existingEmployee = await _unitOfWork.Employees.GetAsync(id);
                if (existingEmployee == null)
                {
                    return Json(new { success = false, message = "Employee not found." });
                }

                // Basic validation
                if (string.IsNullOrEmpty(employee.EmpCode) ||
                    string.IsNullOrEmpty(employee.EmpName) ||
                    employee.ComId == Guid.Empty ||
                    employee.Gross <= 0 ||
                    employee.DtJoin == default)
                {
                    return Json(new { success = false, message = "Required fields are missing." });
                }

                // Related entity validation
                var company = await _unitOfWork.Companies.GetAsync(employee.ComId);
                if (company == null)
                    return Json(new { success = false, message = "Invalid company." });

                if (employee.ShiftId != Guid.Empty && await _unitOfWork.Shifts.GetAsync(employee.ShiftId) == null)
                    return Json(new { success = false, message = "Invalid shift." });

                if (employee.DeptId != Guid.Empty && await _unitOfWork.Departments.GetAsync(employee.DeptId) == null)
                    return Json(new { success = false, message = "Invalid department." });

                if (employee.DesigId != Guid.Empty && await _unitOfWork.Designations.GetAsync(employee.DesigId) == null)
                    return Json(new { success = false, message = "Invalid designation." });

                if (!Enum.IsDefined(typeof(GenderType), employee.Gender))
                    return Json(new { success = false, message = "Invalid gender." });

                // Update employee fields
                existingEmployee.EmpCode = employee.EmpCode;
                existingEmployee.EmpName = employee.EmpName;
                existingEmployee.ComId = employee.ComId;
                existingEmployee.ShiftId = employee.ShiftId;
                existingEmployee.DeptId = employee.DeptId;
                existingEmployee.DesigId = employee.DesigId;
                existingEmployee.Gender = employee.Gender;
                existingEmployee.Gross = employee.Gross;
                existingEmployee.DtJoin = employee.DtJoin;

                // Salary components
                existingEmployee.Basic = employee.Gross * (company.Basic > 0 ? company.Basic : 0.5m);
                existingEmployee.HRent = employee.Gross * (company.HRent > 0 ? company.HRent : 0.3m);
                existingEmployee.Medical = employee.Gross * (company.Medical > 0 ? company.Medical : 0.15m);
                existingEmployee.Others = employee.Gross * 0.05m;

                // Image upload
                if (EmployeeImageFile != null && EmployeeImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images/employees");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + EmployeeImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await EmployeeImageFile.CopyToAsync(fileStream);
                    }

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingEmployee.EmployeeImage))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, existingEmployee.EmployeeImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingEmployee.EmployeeImage = "/images/employees/" + uniqueFileName;
                }

                // Update the employee in the database
                await _unitOfWork.Employees.UpdateAsync(existingEmployee);
                await _unitOfWork.SaveAsync();

                // Include the updated employee in the response
                var updatedEmployee = await _unitOfWork.Employees.GetAsync(id);
                updatedEmployee.Company = await _unitOfWork.Companies.GetAsync(employee.ComId);
                updatedEmployee.Shift = await _unitOfWork.Shifts.GetAsync(employee.ShiftId);
                updatedEmployee.Department = await _unitOfWork.Departments.GetAsync(employee.DeptId);
                updatedEmployee.Designation = await _unitOfWork.Designations.GetAsync(employee.DesigId);

                return Json(new
                {
                    success = true,
                    message = "Employee updated successfully!",
                    employee = new
                    {
                        EmpId = updatedEmployee.EmpId,
                        EmpCode = updatedEmployee.EmpCode,
                        EmpName = updatedEmployee.EmpName,
                        ComId = updatedEmployee.ComId,
                        ShiftId = updatedEmployee.ShiftId,
                        DeptId = updatedEmployee.DeptId,
                        DesigId = updatedEmployee.DesigId,
                        Gender = updatedEmployee.Gender,
                        Gross = updatedEmployee.Gross,
                        Basic = updatedEmployee.Basic,
                        HRent = updatedEmployee.HRent,
                        Medical = updatedEmployee.Medical,
                        Others = updatedEmployee.Others,
                        DtJoin = updatedEmployee.DtJoin,
                        EmployeeImage = updatedEmployee.EmployeeImage,
                        Company = new { ComName = updatedEmployee.Company?.ComName },
                        Shift = new { ShiftName = updatedEmployee.Shift?.ShiftName },
                        Department = new { DeptName = updatedEmployee.Department?.DeptName },
                        Designation = new { DesigName = updatedEmployee.Designation?.DesigName }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EmployeeEdit: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Failed to update employee: {ex.Message}" });
            }
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

        [HttpGet]
        public async Task<IActionResult> GetShiftsByCompany(Guid comId)
        {
            Console.WriteLine($"GetShiftsByCompany called with comId: {comId}");
            var shifts = await _unitOfWork.Shifts.GetAllAsync();
            var filtered = shifts
                .Where(s => s.ComId == comId)
                .Select(s => new { s.ShiftId, ShiftName = s.ShiftName ?? "Unnamed Shift" })
                .ToList();
            Console.WriteLine($"Shifts found: {filtered.Count}");
            return Json(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> GetDeptsByCompany(Guid comId)
        {
            Console.WriteLine($"GetDeptsByCompany called with comId: {comId}");
            var depts = await _unitOfWork.Departments.GetAllAsync();
            var filtered = depts
                .Where(d => d.ComId == comId)
                .Select(d => new { d.DeptId, DeptName = d.DeptName ?? "Unnamed Department" })
                .ToList();
            Console.WriteLine($"Departments found: {filtered.Count}");
            return Json(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationsByCompany(Guid comId)
        {
            Console.WriteLine($"GetDesignationsByCompany called with comId: {comId}");
            var desigs = await _unitOfWork.Designations.GetAllAsync();
            var filtered = desigs
                .Where(d => d.ComId == comId)
                .Select(d => new { d.DesigId, DesigName = d.DesigName ?? "Unnamed Designation" })
                .ToList();
            Console.WriteLine($"Designations found: {filtered.Count}");
            return Json(filtered);
        }
    }
}