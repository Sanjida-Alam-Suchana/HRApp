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
    public class AttendanceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public AttendanceController(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public IActionResult Index()
        {
            Guid? comId = GetCompanyIdFromCookie();
            var attendances = comId.HasValue
                ? _unitOfWork.Attendances.Find(a => a.ComId == comId.Value)
                : _unitOfWork.Attendances.GetAll();
            ViewBag.Employees = _unitOfWork.Employees.GetAll();
            return View(attendances);
        }

        public IActionResult Create()
        {
            ViewBag.Employees = _unitOfWork.Employees.GetAll();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Attendances.Add(attendance);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Employees = _unitOfWork.Employees.GetAll();
            return View(attendance);
        }

        public IActionResult Edit(Guid id)
        {
            var attendance = _unitOfWork.Attendances.GetById(id);
            if (attendance == null)
            {
                return NotFound();
            }
            ViewBag.Employees = _unitOfWork.Employees.GetAll();
            return View(attendance);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Employees = _unitOfWork.Employees.GetAll();
            return View(attendance);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var attendance = _unitOfWork.Attendances.GetById(id);
            if (attendance != null)
            {
                _unitOfWork.Attendances.Delete(attendance);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetAttendances()
        {
            var attendances = _unitOfWork.Attendances.GetAll().Select(a => new { id = a.Id, date = a.dtDate.ToString("yyyy-MM-dd") });
            return Json(attendances);
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