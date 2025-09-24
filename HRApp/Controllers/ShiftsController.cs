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

            // Manually parse StartTime and EndTime from form data
            var startTimeStr = Request.Form["StartTime"];
            var endTimeStr = Request.Form["EndTime"];
            if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                return Json(new { success = false, message = "Start time and end time are required." });

            if (!TimeOnly.TryParse(startTimeStr, out var startTime))
                return Json(new { success = false, message = "Invalid start time format. Use HH:mm." });
            if (!TimeOnly.TryParse(endTimeStr, out var endTime))
                return Json(new { success = false, message = "Invalid end time format. Use HH:mm." });

            shift.ShiftId = Guid.NewGuid();
            shift.StartTime = startTime;
            shift.EndTime = endTime;

            await _unitOfWork.Shifts.AddAsync(shift);
            await _unitOfWork.SaveAsync();

            var company = await _unitOfWork.Companies.GetAsync(shift.ComId);
            System.Diagnostics.Debug.WriteLine($"Shift created: {shift.ShiftName}, Start: {shift.StartTime}, End: {shift.EndTime}, Company: {company?.ComName}");

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftEdit(string shiftId, string shiftName, string startTime, string endTime, string comId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShiftEdit called with: shiftId={shiftId}, shiftName={shiftName}, startTime={startTime}, endTime={endTime}, comId={comId}");

                if (!Guid.TryParse(shiftId, out var guidShiftId) || string.IsNullOrEmpty(shiftName) || string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime) || !Guid.TryParse(comId, out var guidComId))
                    return Json(new { success = false, message = "Invalid input data. Ensure all fields are valid GUIDs and strings." });

                var shift = await _unitOfWork.Shifts.GetAsync(guidShiftId);
                if (shift == null)
                    return Json(new { success = false, message = "Shift not found." });

                if (!TimeOnly.TryParseExact(startTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var startTimeValue))
                    return Json(new { success = false, message = $"Invalid start time format. Use HH:mm (e.g., 09:00). Received: {startTime}" });
                if (!TimeOnly.TryParseExact(endTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var endTimeValue))
                    return Json(new { success = false, message = $"Invalid end time format. Use HH:mm (e.g., 18:00). Received: {endTime}" });

                // Validate startTime < endTime
                if (startTimeValue >= endTimeValue)
                    return Json(new { success = false, message = "End time must be greater than start time." });

                // Check if comId exists
                var company = await _unitOfWork.Companies.GetAsync(guidComId);
                if (company == null)
                    return Json(new { success = false, message = "Invalid company ID." });

                shift.ShiftName = shiftName;
                shift.StartTime = startTimeValue;
                shift.EndTime = endTimeValue;
                shift.ComId = guidComId;

                await _unitOfWork.Shifts.UpdateAsync(shift);
                await _unitOfWork.SaveAsync();

                return Json(new { success = true, message = "Shift updated successfully!" });
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {ex.InnerException?.Message}");
                return Json(new { success = false, message = $"Database error: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShiftEdit exception: {ex.Message}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return Json(new { success = false, message = "Invalid shift ID." });

            var shift = await _unitOfWork.Shifts.GetAsync(guidId);
            if (shift == null)
                return Json(new { success = false, message = "Shift not found." });

            await _unitOfWork.Shifts.RemoveAsync(shift);
            await _unitOfWork.SaveAsync();

            return Json(new { success = true, message = "Shift deleted successfully!" });
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