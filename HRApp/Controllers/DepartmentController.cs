using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRApp.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public DepartmentController(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public IActionResult Index()
        {
            Guid? comId = GetCompanyIdFromCookie();
            var departments = comId.HasValue
                ? _unitOfWork.Departments.Find(d => d.ComId == comId.Value)
                : _unitOfWork.Departments.GetAll();
            return View(departments);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Departments.Add(department);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        public IActionResult Edit(Guid id)
        {
            var department = _unitOfWork.Departments.GetById(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Department department)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Departments.Update(department);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var department = _unitOfWork.Departments.GetById(id);
            if (department != null)
            {
                _unitOfWork.Departments.Delete(department);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetDepartments()
        {
            var departments = _unitOfWork.Departments.GetAll().Select(d => new { id = d.Id, name = d.DeptName });
            return Json(departments);
        }

        private Guid? GetCompanyIdFromCookie()
        {
            if (_cache.TryGetValue("SelectedCompanyId", out Guid comId))
            {
                return comId;
            }
            return null;
        }
    }
}