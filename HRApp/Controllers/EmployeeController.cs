using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRApp.Controllers
{
    public class EmployeeController(IUnitOfWork unitOfWork, IMemoryCache cache) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private static readonly string[] data = ["Employee not found"];
        private static readonly string[] dataArray = ["Employee not found"];

        public IActionResult Index()
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            ViewBag.Designations = _unitOfWork.Designations.GetAll().ToList();
            ViewBag.Departments = _unitOfWork.Departments.GetAll().ToList();
            ViewBag.Shifts = _unitOfWork.Shifts.GetAll().ToList();
            var employees = _unitOfWork.Employees.GetAll();
            if (ViewBag.SelectedCompanyId != Guid.Empty)
            {
                employees = [.. employees.Where(e => e.ComId == ViewBag.SelectedCompanyId)];
            }
            return View(employees);
        }

        public IActionResult Create()
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            ViewBag.Designations = _unitOfWork.Designations.GetAll().ToList();
            ViewBag.Departments = _unitOfWork.Departments.GetAll().ToList();
            ViewBag.Shifts = _unitOfWork.Shifts.GetAll().ToList();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.Employees.Add(employee);
                    await _unitOfWork.SaveAsync();
                    SetCompanyIdInCookie(employee.ComId);
                    return Json(new { success = true, message = "Employee added successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errors = new[] { "Save failed: " + ex.Message } });
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray() });
        }

        public IActionResult Edit(Guid id)
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            ViewBag.Designations = _unitOfWork.Designations.GetAll().ToList();
            ViewBag.Departments = _unitOfWork.Departments.GetAll().ToList();
            ViewBag.Shifts = _unitOfWork.Shifts.GetAll().ToList();
            var employee = _unitOfWork.Employees.GetById(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost]
        public async Task<JsonResult> Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingEmployee = _unitOfWork.Employees.GetById(employee.Id);
                    if (existingEmployee != null)
                    {
                        existingEmployee.EmpCode = employee.EmpCode;
                        existingEmployee.EmpName = employee.EmpName;
                        existingEmployee.ShiftId = employee.ShiftId;
                        existingEmployee.DeptId = employee.DeptId;
                        existingEmployee.DesigId = employee.DesigId;
                        existingEmployee.Gender = employee.Gender;
                        existingEmployee.Gross = employee.Gross;
                        existingEmployee.Basic = employee.Basic;
                        existingEmployee.HRent = employee.HRent;
                        existingEmployee.Medical = employee.Medical;
                        existingEmployee.Others = employee.Others;
                        existingEmployee.dtJoin = employee.dtJoin;
                        _unitOfWork.Employees.Update(existingEmployee);
                        await _unitOfWork.SaveAsync();
                        return Json(new { success = true, message = "Employee updated successfully!" });
                    }
                    return Json(new { success = false, errors = data });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errors = new[] { "Update failed: " + ex.Message } });
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray() });
        }

        [HttpPost]
        public async Task<JsonResult> Delete(Guid id)
        {
            var employee = _unitOfWork.Employees.GetById(id);
            if (employee != null)
            {
                try
                {
                    _unitOfWork.Employees.Delete(employee);
                    await _unitOfWork.SaveAsync();
                    return Json(new { success = true, message = "Employee deleted successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errors = new[] { "Delete failed: " + ex.Message } });
                }
            }
            return Json(new { success = false, errors = dataArray });
        }

        [HttpGet]
        public JsonResult GetEmployees()
        {
            var employees = _unitOfWork.Employees.GetAll().Select(e => new
            {
                id = e.Id,
                empCode = e.EmpCode,
                empName = e.EmpName,
                shiftId = e.ShiftId,
                deptId = e.DeptId,
                desigId = e.DesigId,
                gender = e.Gender,
                gross = e.Gross,
                basic = e.Basic,
                hrent = e.HRent,
                medical = e.Medical,
                others = e.Others,
                dtJoin = e.dtJoin.ToString("yyyy-MM-dd")
            });
            return Json(employees);
        }

        private void SetCompanyIdInCookie(Guid comId)
        {
            _cache.Set("SelectedCompanyId", comId, TimeSpan.FromDays(1));
        }
    }
}