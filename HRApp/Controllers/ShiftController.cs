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
    public class ShiftController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public ShiftController(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public IActionResult Index()
        {
            Guid? comId = GetCompanyIdFromCookie();
            var shifts = comId.HasValue
                ? _unitOfWork.Shifts.Find(s => s.ComId == comId.Value)
                : _unitOfWork.Shifts.GetAll();
            return View(shifts);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Shift shift)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Shifts.Add(shift);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        public IActionResult Edit(Guid id)
        {
            var shift = _unitOfWork.Shifts.GetById(id);
            if (shift == null)
            {
                return NotFound();
            }
            return View(shift);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Shift shift)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Shifts.Update(shift);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var shift = _unitOfWork.Shifts.GetById(id);
            if (shift != null)
            {
                _unitOfWork.Shifts.Delete(shift);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetShifts()
        {
            var shifts = _unitOfWork.Shifts.GetAll().Select(s => new { id = s.Id, name = s.ShiftName });
            return Json(shifts);
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