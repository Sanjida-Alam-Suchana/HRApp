using Microsoft.AspNetCore.Mvc;
using HRApp.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace HRApp.Controllers
{
    public class HomeController(IUnitOfWork unitOfWork, IMemoryCache cache) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;

        public IActionResult Index()
        {
            ViewBag.Companies = _unitOfWork.Companies.GetAll().Select(c => new { c.Id, c.ComName }).ToList();
            ViewBag.SelectedCompanyId = _cache.TryGetValue("SelectedCompanyId", out Guid selectedId) ? selectedId : Guid.Empty;
            return View();
        }

        [HttpPost]
        public JsonResult SetSelectedCompany(Guid companyId)
        {
            _cache.Set("SelectedCompanyId", companyId, TimeSpan.FromDays(1));
            return Json(new { success = true });
        }
    }
}