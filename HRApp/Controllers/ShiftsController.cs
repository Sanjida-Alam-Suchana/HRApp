using System;
using System.Linq;
using System.Threading.Tasks;
using HRApp.Models;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRApp.Controllers
{
    public class ShiftsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShiftsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Shifts/ShiftIndex
        public async Task<IActionResult> ShiftIndex()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            var allShifts = await _unitOfWork.Shifts.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"Fetched {allShifts?.Count() ?? 0} shifts from database.");
            var shifts = string.IsNullOrEmpty(selectedComId)
                ? allShifts
                : allShifts.Where(s => s.ComId != Guid.Empty && s.ComId.ToString() == selectedComId).ToList();

            // Validate company mappings
            foreach (var shift in shifts)
            {
                if (!companies.Any(c => c.ComId == shift.ComId))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Shift with ID {shift.ShiftId} has ComId {shift.ComId} not found in companies.");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Filtered shifts count: {shifts?.Count() ?? 0}");
            return View(shifts);
        }

        // GET: Shifts/ShiftCreate
        public async Task<IActionResult> ShiftCreate()
        {
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftCreate(Shift shift)
        {
            if (string.IsNullOrEmpty(shift.ShiftName))
                return Json(new { success = false, message = "Shift name is required." });
            if (shift.ComId == Guid.Empty)
                return Json(new { success = false, message = "Please select a company." });

            shift.ShiftId = Guid.NewGuid();
            await _unitOfWork.Shifts.AddAsync(shift);
            await _unitOfWork.SaveAsync();

            var company = await _unitOfWork.Companies.GetAsync(shift.ComId);
            return Json(new
            {
                success = true,
                message = "Shift created successfully!",
                shift = new
                {
                    ShiftId = shift.ShiftId.ToString(),
                    shift.ShiftName,
                    StartTime = shift.StartTime.ToString("HH:mm"),
                    EndTime = shift.EndTime.ToString("HH:mm"),
                    shift.ComId,
                    ComName = company?.ComName ?? "N/A"
                }
            });
        }

        // GET: Shifts/ShiftEdit/{id}
        public async Task<IActionResult> ShiftEdit(Guid id)
        {
            var shift = await _unitOfWork.Shifts.GetAsync(id);
            if (shift == null) return NotFound();
            ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftEdit(Guid id, Shift shift)
        {
            if (id != shift.ShiftId)
                return Json(new { success = false, message = "Invalid shift ID." });

            var existingShift = await _unitOfWork.Shifts.GetAsync(id);
            if (existingShift == null)
                return Json(new { success = false, message = "Shift not found." });

            existingShift.ShiftName = shift.ShiftName;
            existingShift.StartTime = shift.StartTime;
            existingShift.EndTime = shift.EndTime;
            existingShift.ComId = shift.ComId;

            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Shift updated!" });
        }


        // POST: Shifts/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var shift = await _unitOfWork.Shifts.GetAsync(id);
            if (shift == null)
            {
                return Json(new { success = false, message = "Shift not found." });
            }
            await _unitOfWork.Shifts.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            System.Diagnostics.Debug.WriteLine($"Shift deleted with ID: {id}");
            return Json(new { success = true, message = "Shift deleted!" });
        }

        // GET: Filter by company (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetShiftsByCompany(Guid comId)
        {
            try
            {
                Console.WriteLine($"Fetching shifts for ComId {comId} at {DateTime.Now}");
                var shifts = _unitOfWork.Shifts.GetAll(); // IQueryable
                var result = await shifts
                    .Where(s => s.ComId == comId)  
                    .Select(s => new
                    {
                        ShiftId = s.ShiftId.ToString(),
                        s.ShiftName,
                        StartTime = s.StartTime.ToString("HH:mm"),
                        EndTime = s.EndTime.ToString("HH:mm"),
                        s.ComId,
                        ComName = s.Company != null ? s.Company.ComName : "N/A"
                    }).ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No shifts found for this company.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetShiftsByCompany: {ex}");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllShifts()
        {
            try
            {
                Console.WriteLine($"Fetching all shifts at {DateTime.Now}");
                var shifts = _unitOfWork.Shifts.GetAll(); // IQueryable
                var result = await shifts

                     .Select(s => new
                     {
                         ShiftId = s.ShiftId.ToString(),
                         s.ShiftName,
                         StartTime = s.StartTime.ToString("HH:mm"),
                         EndTime = s.EndTime.ToString("HH:mm"),
                         s.ComId,
                         ComName = s.Company != null ? s.Company.ComName : "N/A"
                     }).ToListAsync();

                if (!result.Any())
                    Console.WriteLine("No Shifts found.");

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllShifts: {ex}");
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
    }
}