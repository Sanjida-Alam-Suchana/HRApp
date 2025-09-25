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
            ViewBag.SelectedComId = Request.Cookies["SelectedComId"];

            var selectedComId = Request.Cookies["SelectedComId"];
            var allShifts = await _unitOfWork.Shifts.GetAll().Include(s => s.Company).ToListAsync();
            System.Diagnostics.Debug.WriteLine($"Fetched {allShifts?.Count() ?? 0} shifts from database.");
            var shifts = string.IsNullOrEmpty(selectedComId)
                ? allShifts
                : allShifts.Where(s => s.ComId != Guid.Empty && s.ComId.ToString() == selectedComId).ToList();

            foreach (var shift in shifts)
            {
                if (shift.ComId != Guid.Empty && !companies.Any(c => c.ComId == shift.ComId))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Shift with ID {shift.ShiftId} has invalid ComId {shift.ComId}.");
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

        // POST: Shifts/ShiftCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftCreate([FromForm] Shift shift)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShiftCreate called with: ShiftName={shift.ShiftName}, StartTime={shift.StartTime}, EndTime={shift.EndTime}, ComId={shift.ComId}");

                if (string.IsNullOrWhiteSpace(shift.ShiftName))
                    return Json(new { success = false, message = "Shift name is required." });
                if (shift.ComId == Guid.Empty)
                    return Json(new { success = false, message = "Please select a company." });
                if (shift.StartTime == default || shift.EndTime == default)
                    return Json(new { success = false, message = "Start time and end time are required." });
                if (shift.StartTime >= shift.EndTime)
                    return Json(new { success = false, message = "End time must be after start time." });

                var company = await _unitOfWork.Companies.GetAsync(shift.ComId);
                if (company == null)
                    return Json(new { success = false, message = "Selected company does not exist." });

                shift.ShiftId = Guid.NewGuid();
                await _unitOfWork.Shifts.AddAsync(shift);
                await _unitOfWork.SaveAsync();

                System.Diagnostics.Debug.WriteLine($"Shift created: {shift.ShiftName}, Start: {shift.StartTime}, End: {shift.EndTime}, Company: {company.ComName}");

                return Json(new
                {
                    success = true,
                    message = "Shift created successfully!",
                    shift = new
                    {
                        shiftId = shift.ShiftId.ToString(),
                        shift.ShiftName,
                        startTime = shift.StartTime.ToString("HH:mm"),
                        endTime = shift.EndTime.ToString("HH:mm"),
                        shift.ComId,
                        comName = company.ComName // Always return valid company name
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShiftCreate exception: {ex.Message}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Shifts/GetShift/{id}
        [HttpGet]
        public async Task<IActionResult> GetShift(Guid id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Fetching shift for id: {id} at {DateTime.Now}");
                var shift = await _unitOfWork.Shifts.GetAll().Include(s => s.Company).FirstOrDefaultAsync(s => s.ShiftId == id);
                if (shift == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Shift not found for id: {id}");
                    return NotFound(new { success = false, message = "Shift not found." });
                }

                return Json(new
                {
                    success = true,
                    shift = new
                    {
                        shiftId = shift.ShiftId.ToString(),
                        shift.ShiftName,
                        startTime = shift.StartTime.ToString("HH:mm"),
                        endTime = shift.EndTime.ToString("HH:mm"),
                        shift.ComId,
                        comName = shift.Company?.ComName ?? "N/A" // Fallback only if Company is null
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetShift at {DateTime.Now}: {ex}");
                return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
            }
        }

        // POST: Shifts/ShiftEdit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftEdit(Guid id, [FromForm] Shift shift)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShiftEdit called with: ShiftId={id}, ShiftName={shift.ShiftName}, StartTime={shift.StartTime}, EndTime={shift.EndTime}, ComId={shift.ComId}");

                if (id != shift.ShiftId)
                    return Json(new { success = false, message = "Invalid ID." });
                if (string.IsNullOrWhiteSpace(shift.ShiftName))
                    return Json(new { success = false, message = "Shift name is required." });
                if (shift.ComId == Guid.Empty)
                    return Json(new { success = false, message = "Please select a company." });
                if (shift.StartTime == default || shift.EndTime == default)
                    return Json(new { success = false, message = "Start time and end time are required." });
                if (shift.StartTime >= shift.EndTime)
                    return Json(new { success = false, message = "End time must be after start time." });

                var existing = await _unitOfWork.Shifts.GetAll().Include(s => s.Company).FirstOrDefaultAsync(s => s.ShiftId == id);
                if (existing == null)
                    return Json(new { success = false, message = "Shift not found." });

                var company = await _unitOfWork.Companies.GetAsync(shift.ComId);
                if (company == null)
                    return Json(new { success = false, message = "Selected company does not exist." });

                existing.ShiftName = shift.ShiftName;
                existing.StartTime = shift.StartTime;
                existing.EndTime = shift.EndTime;
                existing.ComId = shift.ComId;

                await _unitOfWork.SaveAsync();

                System.Diagnostics.Debug.WriteLine($"Shift updated: {existing.ShiftName}, Start: {existing.StartTime}, End: {existing.EndTime}, Company: {company.ComName}");

                return Json(new
                {
                    success = true,
                    message = "Shift updated successfully!",
                    shift = new
                    {
                        shiftId = existing.ShiftId.ToString(),
                        existing.ShiftName,
                        startTime = existing.StartTime.ToString("HH:mm"),
                        endTime = existing.EndTime.ToString("HH:mm"),
                        existing.ComId,
                        comName = company.ComName // Always return valid company name
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShiftEdit exception: {ex.Message}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // POST: Shifts/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Delete called with shiftId={id}");

                var shift = await _unitOfWork.Shifts.GetAsync(id);
                if (shift == null)
                    return Json(new { success = false, message = "Shift not found." });

                await _unitOfWork.Shifts.RemoveAsync(shift);
                await _unitOfWork.SaveAsync();

                System.Diagnostics.Debug.WriteLine($"Shift deleted: {shift.ShiftName}");
                return Json(new { success = true, message = "Shift deleted successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete exception: {ex.Message}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Shifts/GetShiftsByCompany
        [HttpGet]
        public async Task<IActionResult> GetShiftsByCompany(Guid comId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Fetching shifts for ComId {comId} at {DateTime.Now}");
                var shifts = await _unitOfWork.Shifts.GetAll().Include(s => s.Company).ToListAsync();
                var result = shifts
                    .Where(s => s.ComId == comId)
                    .Select(s => new
                    {
                        shiftId = s.ShiftId.ToString(),
                        s.ShiftName,
                        startTime = s.StartTime.ToString("HH:mm"),
                        endTime = s.EndTime.ToString("HH:mm"),
                        s.ComId,
                        comName = s.Company?.ComName ?? "N/A" // Fallback only if Company is null
                    }).ToList();

                System.Diagnostics.Debug.WriteLine($"Found {result.Count} shifts for ComId {comId}");
                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetShiftsByCompany: {ex.Message}");
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        // GET: Shifts/GetAllShifts
        [HttpGet]
        public async Task<IActionResult> GetAllShifts()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Fetching all shifts at {DateTime.Now}");
                var shifts = await _unitOfWork.Shifts.GetAll().Include(s => s.Company).ToListAsync();
                var result = shifts
                    .Select(s => new
                    {
                        shiftId = s.ShiftId.ToString(),
                        s.ShiftName,
                        startTime = s.StartTime.ToString("HH:mm"),
                        endTime = s.EndTime.ToString("HH:mm"),
                        s.ComId,
                        comName = s.Company?.ComName ?? "N/A" // Fallback only if Company is null
                    }).ToList();

                System.Diagnostics.Debug.WriteLine($"Found {result.Count} shifts");
                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllShifts: {ex.Message}");
                return Json(new { success = false, message = $"Server error: {ex.Message}" });
            }
        }
    }
}