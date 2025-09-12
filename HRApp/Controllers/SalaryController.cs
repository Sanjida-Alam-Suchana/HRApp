using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Controllers
{
    public class SalaryController(IUnitOfWork unitOfWork, IMemoryCache cache, IConfiguration configuration) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection"); // made nullable

        public IActionResult Index()
        {
            var salaries = _unitOfWork.Salaries.GetAllQueryable()
                .Include(s => s.Employee)
                .ToList();
            return View(salaries);
        }

        [HttpGet]
        public IActionResult Generate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(int year, int month)
        {
            Guid? comId = GetCompanyIdFromCookie();
            if (comId.HasValue && _connectionString != null)
            {
                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand("CALL CalculateSalary(@comid, @year, @month)", conn);
                cmd.Parameters.AddWithValue("comid", comId.Value);
                cmd.Parameters.AddWithValue("year", year);
                cmd.Parameters.AddWithValue("month", month);
                await cmd.ExecuteNonQueryAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var salary = _unitOfWork.Salaries.GetById(id);
            if (salary != null)
            {
                salary.IsPaid = true;
                salary.PaidAmount = salary.PayableAmount;
                _unitOfWork.Salaries.Update(salary);
                await _unitOfWork.SaveAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetSalaries()
        {
            var salaries = _unitOfWork.Salaries.GetAllQueryable()
                .Include(s => s.Employee)
                .AsEnumerable() // FIX applied
                .Select(s => new
                {
                    id = s.Id,
                    empName = s.Employee?.EmpName ?? "N/A",
                    year = s.dtYear,
                    month = s.dtMonth,
                    gross = s.Gross,
                    basic = s.Basic,
                    hrent = s.HRent,
                    medical = s.Medical,
                    absentAmount = s.AbsentAmount,
                    payable = s.PayableAmount,
                    isPaid = s.IsPaid
                }).ToList();

            return Json(salaries);
        }

        private Guid? GetCompanyIdFromCookie()
        {
            if (_cache.TryGetValue("SelectedCompanyId", out Guid comId)) return comId;
            return null;
        }
    }
}
