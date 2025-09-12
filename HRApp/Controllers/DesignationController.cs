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
    public class DesignationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public DesignationController(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public IActionResult Index()
        {
            Guid? comId = GetCompanyIdFromCookie();
            var designations = comId.HasValue
                ? _unitOfWork.Designations.Find(d => d.ComId == comId.Value)
                : _unitOfWork.Designations.GetAll();
            return View(designations);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Designation designation)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Designations.Add(designation);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(designation);
        }

        public IActionResult Edit(Guid id)
        {
            var designation = _unitOfWork.Designations.GetById(id);
            if (designation == null)
            {
                return NotFound();
            }
            return View(designation);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Designation designation)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Designations.Update(designation);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(designation);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var designation = _unitOfWork.Designations.GetById(id);
            if (designation != null)
            {
                _unitOfWork.Designations.Delete(designation);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetDesignations()
        {
            var designations = _unitOfWork.Designations.GetAll().Select(d => new { id = d.Id, name = d.DesigName });
            return Json(designations);
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