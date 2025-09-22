using HRApp.Data;
using HRApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace HRApp.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EmployeesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Employees/Index
        public IActionResult EmployeeIndex()
        {
            // Load all employees with related data
            var employees = _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .ToList();

            // Pass companies for filter dropdown
            ViewBag.Companies = _context.Companies.ToList();

            return View(employees);
        }

        // GET: Employees/GetEmployees?comId={guid}
        [HttpGet]
        public IActionResult GetEmployees(Guid? comId)
        {
            var employees = _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Where(e => !comId.HasValue || e.ComId == comId.Value)
                .ToList();

            return Json(employees.Select(e => new
            {
                EmpId = e.EmpId,
                EmpCode = e.EmpCode,
                EmpName = e.EmpName,
                Company = e.Company.ComName,
                Shift = e.Shift.ShiftName,
                Department = e.Department.DeptName,
                Designation = e.Designation.DesigName,
                Gender = e.Gender.ToString(),
                Gross = e.Gross,
                Basic = e.Basic,
                HRent = e.HRent,
                Medical = e.Medical,
                Others = e.Others,
                DtJoin = e.DtJoin,
                EmployeeImage = e.EmployeeImage
            }));
        }

        // GET: Employees/GetCompanies
        [HttpGet]
        public IActionResult GetCompanies()
        {
            var companies = _context.Companies.Select(c => new
            {
                ComId = c.ComId,
                ComName = c.ComName
            }).ToList();

            return Json(companies);
        }

        // GET: Employees/GetCompanyDetails/{id}
        [HttpGet]
        public IActionResult GetCompanyDetails(Guid id)
        {
            var company = _context.Companies.Find(id);
            if (company == null)
            {
                return NotFound();
            }

            return Json(new
            {
                Basic = company.Basic,
                HRent = company.HRent,
                Medical = company.Medical
            });
        }

        // GET: Employees/GetShifts?comId={guid}
        [HttpGet]
        public IActionResult GetShifts(Guid comId)
        {
            var shifts = _context.Shifts
                .Where(s => s.ComId == comId)
                .Select(s => new
                {
                    ShiftId = s.ShiftId,
                    ShiftName = s.ShiftName
                }).ToList();

            return Json(shifts);
        }

        // GET: Employees/GetDepartments?comId={guid}
        [HttpGet]
        public IActionResult GetDepartments(Guid comId)
        {
            var departments = _context.Departments
                .Where(d => d.ComId == comId)
                .Select(d => new
                {
                    DeptId = d.DeptId,
                    DeptName = d.DeptName
                }).ToList();

            return Json(departments);
        }

        // GET: Employees/GetDesignations?comId={guid}
        [HttpGet]
        public IActionResult GetDesignations(Guid comId)
        {
            var designations = _context.Designations
                .Where(d => d.ComId == comId)
                .Select(d => new
                {
                    DesigId = d.DesigId,
                    DesigName = d.DesigName
                }).ToList();

            return Json(designations);
        }

        // GET: Employees/GetEmployee/{id}
        [HttpGet]
        public IActionResult GetEmployee(Guid id)
        {
            var employee = _context.Employees
                .Include(e => e.Company)
                .Include(e => e.Shift)
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefault(e => e.EmpId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                EmpId = employee.EmpId,
                EmpCode = employee.EmpCode,
                EmpName = employee.EmpName,
                ComId = employee.ComId,
                ShiftId = employee.ShiftId,
                DeptId = employee.DeptId,
                DesigId = employee.DesigId,
                Gender = (int)employee.Gender,
                Gross = employee.Gross,
                Basic = employee.Basic,
                HRent = employee.HRent,
                Medical = employee.Medical,
                Others = employee.Others,
                DtJoin = employee.DtJoin.ToString("yyyy-MM-dd"),
                EmployeeImage = employee.EmployeeImage
            });
        }

        // POST: Employees/Create
        [HttpPost]
       // [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] EmployeeDTO employee, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Invalid data.", errors = errors });
            }

            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images/employees");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                employee.EmployeeImage = "/images/employees/" + uniqueFileName;
            }
            var newEmployee = new Employee
            {
                
                EmpCode = employee.EmpCode,
                EmpName = employee.EmpName,
                ComId= employee.ComId,
                DesigId= employee.DesigId,
                DeptId= employee.DeptId,
                ShiftId = employee.ShiftId,
                Gender = employee.Gender,
                Gross = employee.Gross,
                Basic = employee.Basic,
                HRent = employee.HRent,
                Medical = employee.Medical,
                Others = employee.Others,
                DtJoin = employee.DtJoin,
                EmployeeImage = employee.EmployeeImage
            };

            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();



            return Json(new { success = true, message = "Employee created successfully.", employee = newEmployee });
        }
        // POST: Employees/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [FromForm] Employee model, IFormFile? imageFile)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Invalid data.", errors = errors });
            }

            employee.EmpCode = model.EmpCode;
            employee.EmpName = model.EmpName;
            employee.ComId = model.ComId;
            employee.ShiftId = model.ShiftId;
            employee.DeptId = model.DeptId;
            employee.DesigId = model.DesigId;
            employee.Gender = model.Gender;
            employee.Gross = model.Gross;
            employee.Basic = model.Basic;
            employee.HRent = model.HRent;
            employee.Medical = model.Medical;
            employee.Others = model.Others;
            employee.DtJoin = model.DtJoin;

            if (imageFile != null && imageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(employee.EmployeeImage))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, employee.EmployeeImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images/employees");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                employee.EmployeeImage = "/images/employees/" + uniqueFileName;
            }

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            var updatedEmployee = new
            {
                EmpId = employee.EmpId,
                EmpCode = employee.EmpCode,
                EmpName = employee.EmpName,
                Company = _context.Companies.Find(employee.ComId)?.ComName,
                Shift = _context.Shifts.Find(employee.ShiftId)?.ShiftName,
                Department = _context.Departments.Find(employee.DeptId)?.DeptName,
                Designation = _context.Designations.Find(employee.DesigId)?.DesigName,
                Gender = employee.Gender.ToString(),
                Gross = employee.Gross,
                Basic = employee.Basic,
                HRent = employee.HRent,
                Medical = employee.Medical,
                Others = employee.Others,
                DtJoin = employee.DtJoin,
                EmployeeImage = employee.EmployeeImage
            };

            return Json(new { success = true, message = "Employee updated successfully.", employee = updatedEmployee });
        }

        // POST: Employees/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            // Delete image if exists
            if (!string.IsNullOrEmpty(employee.EmployeeImage))
            {
                var filePath = Path.Combine(_env.WebRootPath, employee.EmployeeImage.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Remove from database
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Employee deleted successfully." });
        }

    }
}