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
    public class CompanyController(IUnitOfWork unitOfWork, IMemoryCache cache) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private static readonly string[] data = ["Company not found"];
        private static readonly string[] dataArray = ["Company not found"];

        public IActionResult Index()
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            var companies = _unitOfWork.Companies.GetAll();
            if (ViewBag.SelectedCompanyId != Guid.Empty)
            {
                companies = [.. companies.Where(c => c.Id == ViewBag.SelectedCompanyId)];
            }
            return View(companies);
        }

        public IActionResult Create()
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Create(Company company)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.Companies.Add(company);
                    await _unitOfWork.SaveAsync();
                    SetCompanyIdInCookie(company.Id);
                    return Json(new { success = true, message = "Company added successfully!" });
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
            var company = _unitOfWork.Companies.GetById(id);
            if (company == null) return NotFound();
            return View(company);
        }

        [HttpPost]
        public async Task<JsonResult> Edit(Company company)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingCompany = _unitOfWork.Companies.GetById(company.Id);
                    if (existingCompany != null)
                    {
                        existingCompany.ComName = company.ComName;
                        existingCompany.Basic = company.Basic;
                        existingCompany.HRent = company.HRent;
                        existingCompany.Medical = company.Medical;
                        existingCompany.IsInactive = company.IsInactive;
                        _unitOfWork.Companies.Update(existingCompany);
                        await _unitOfWork.SaveAsync();
                        return Json(new { success = true, message = "Company updated successfully!" });
                    }
                    return Json(new { success = false, errors = dataArray });
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
            var company = _unitOfWork.Companies.GetById(id);
            if (company != null)
            {
                try
                {
                    _unitOfWork.Companies.Delete(company);
                    await _unitOfWork.SaveAsync();
                    return Json(new { success = true, message = "Company deleted successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, errors = new[] { "Delete failed: " + ex.Message } });
                }
            }
            return Json(new { success = false, errors = data });
        }

        [HttpGet]
        public JsonResult GetCompanies()
        {
            var companies = _unitOfWork.Companies.GetAll().Select(c => new { id = c.Id, name = c.ComName, basic = c.Basic, hrent = c.HRent, medical = c.Medical, isInactive = c.IsInactive });
            return Json(companies);
        }

        private void SetCompanyIdInCookie(Guid comId)
        {
            _cache.Set("SelectedCompanyId", comId, TimeSpan.FromDays(1));
        }

        private Guid? GetCompanyIdFromCookie()
        {
            if (_cache.TryGetValue("SelectedCompanyId", out Guid comId)) return comId;
            return null;
        }
    }
}
/*using HRApp.Models;
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
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public CompanyController(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public IActionResult Index()
        {
            Guid? comId = GetCompanyIdFromCookie();
            var companies = comId.HasValue
                ? _unitOfWork.Companies.Find(c => c.Id == comId.Value)
                : _unitOfWork.Companies.GetAll();
            return View(companies);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Company company)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Companies.Add(company);
                await _unitOfWork.SaveAsync();
                SetCompanyIdInCookie(company.Id);
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        public IActionResult Edit(Guid id)
        {
            var company = _unitOfWork.Companies.GetById(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Company company)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Companies.Update(company);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var company = _unitOfWork.Companies.GetById(id);
            if (company != null)
            {
                _unitOfWork.Companies.Delete(company);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetCompanies()
        {
            var companies = _unitOfWork.Companies.GetAll().Select(c => new { id = c.Id, name = c.ComName });
            return Json(companies);
        }

        private Guid? GetCompanyIdFromCookie()
        {
            if (_cache.TryGetValue("SelectedCompanyId", out Guid comId))
            {
                return comId;
            }
            return null;
        }

        private void SetCompanyIdInCookie(Guid comId)
        {
            _cache.Set("SelectedCompanyId", comId, TimeSpan.FromDays(1));
        }
    }
}*/