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
    public class AttendanceSummaryController(IUnitOfWork unitOfWork, IMemoryCache cache, IConfiguration configuration) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection"); // made nullable

        public IActionResult Index()
        {
            var summaries = _unitOfWork.AttendanceSummaries.GetAllQueryable()
                .Include(s => s.Employee)
                .ToList();
            return View(summaries);
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
                using var cmd = new NpgsqlCommand("CALL SummarizeAttendance(@comid, @year, @month)", conn);
                cmd.Parameters.AddWithValue("comid", comId.Value);
                cmd.Parameters.AddWithValue("year", year);
                cmd.Parameters.AddWithValue("month", month);
                await cmd.ExecuteNonQueryAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public JsonResult GetSummaries()
        {
            var summaries = _unitOfWork.AttendanceSummaries.GetAllQueryable()
                .Include(s => s.Employee)
                .AsEnumerable() 
                .Select(s => new
                {
                    id = s.Id,
                    empName = s.Employee?.EmpName ?? "N/A",
                    year = s.dtYear,
                    month = s.dtMonth,
                    present = s.Present,
                    late = s.Late,
                    absent = s.Absent
                }).ToList();

            return Json(summaries);
        }

        private Guid? GetCompanyIdFromCookie()
        {
            if (_cache.TryGetValue("SelectedCompanyId", out Guid comId)) return comId;
            return null;
        }
    }
}
