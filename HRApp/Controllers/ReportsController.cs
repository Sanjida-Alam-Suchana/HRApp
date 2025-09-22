using CsvHelper;
using CsvHelper.Configuration;
using HRApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options; // Use this for Margins and Size

namespace HRApp.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // First Report: Employee List
        public async Task<IActionResult> EmployeeList()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            ViewBag.SelectedComId = selectedComId;

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetDepartmentsByCompany(Guid companyId)
        {
            try
            {
                var departments = await _unitOfWork.Departments
                    .GetQueryable()
                    .Where(d => d.ComId == companyId)
                    .Select(d => new { id = d.DeptId, name = d.DeptName })
                    .OrderBy(d => d.name)
                    .ToListAsync();

                return Json(departments); 
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        //[HttpGet]
        //public async Task<IActionResult> GetDepartmentsByCompany(Guid companyId)
        //{
        //    var departments = await _unitOfWork.Departments.GetAllAsync();
        //    var filtered = departments.Where(d => d.ComId == companyId)
        //        .Select(d => new { Id = d.DeptId, Name = d.DeptName });
        //    return Json(filtered);
        //}

        [HttpPost]
        public async Task<IActionResult> GetEmployeeList(Guid? companyId, Guid? departmentId)
        {
            if (!companyId.HasValue) return BadRequest("Company required.");

            var query = _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.Shift)
                .Where(e => e.ComId == companyId.Value);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                query = query.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await query.ToListAsync();

            var result = employees.Select(e => new
            {
                EmployeeName = e.EmpName,
                JoinDate = e.DtJoin.ToShortDateString(),
                ServiceDays = (DateTime.Now - e.DtJoin).Days,
                DepartmentName = e.Department?.DeptName ?? "N/A",
                DesignationName = e.Designation?.DesigName ?? "N/A",
                ShiftName = e.Shift?.ShiftName ?? "N/A"
            });

            return Json(result);
        }

        // Download actions (CSV, Excel, PDF)
        [HttpGet]
        public async Task<IActionResult> DownloadEmployeeListCsv(Guid companyId, Guid? departmentId)
        {
            var data = await GetEmployeeDataAsync(companyId, departmentId);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
            await csv.WriteRecordsAsync(data);
            await writer.FlushAsync();
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "text/csv", "EmployeeList.csv");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEmployeeListExcel(Guid companyId, Guid? departmentId)
        {
            var data = await GetEmployeeDataAsync(companyId, departmentId);
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("EmployeeList");
            worksheet.Cells.LoadFromCollection(data, true);
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeList.xlsx");
        }

        [HttpGet] // Remove duplicate [HttpGet]
        public async Task<IActionResult> DownloadEmployeeListPdf(Guid companyId, Guid? departmentId)
        {
            var data = await GetEmployeeDataAsync(companyId, departmentId);
            return new ViewAsPdf("EmployeeListPdf", data) // Use Rotativa.AspNetCore.ViewAsPdf
            {
                FileName = "EmployeeList.pdf",
                PageSize = Size.A4, // Use Rotativa.AspNetCore.Options.Size
                PageMargins = new Margins(10, 10, 10, 10) // Use Rotativa.AspNetCore.Options.Margins
            };
        }

        private async Task<IEnumerable<object>> GetEmployeeDataAsync(Guid companyId, Guid? departmentId)
        {
            var query = _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.Shift)
                .Where(e => e.ComId == companyId);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                query = query.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await query.ToListAsync();

            return employees.Select(e => new
            {
                EmployeeName = e.EmpName,
                JoinDate = e.DtJoin.ToShortDateString(),
                ServiceDays = (DateTime.Now - e.DtJoin).Days,
                DepartmentName = e.Department?.DeptName ?? "N/A",
                DesignationName = e.Designation?.DesigName ?? "N/A",
                ShiftName = e.Shift?.ShiftName ?? "N/A"
            });
        }

        // Second Report: Attendance List
        public async Task<IActionResult> AttendanceList()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            ViewBag.SelectedComId = selectedComId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetAttendanceList(Guid? companyId, Guid? departmentId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                if (!companyId.HasValue || companyId == Guid.Empty)
                    return Json(new { success = false, message = "Company required." });

                if (!fromDate.HasValue || !toDate.HasValue || fromDate > toDate)
                    return Json(new { success = false, message = "Invalid date range." });

                var totalDays = (toDate.Value.Date - fromDate.Value.Date).Days + 1;

                // Convert DateTime to DateOnly for comparison
                var fromDateOnly = DateOnly.FromDateTime(fromDate.Value.Date);
                var toDateOnly = DateOnly.FromDateTime(toDate.Value.Date);

                // Get employees by department filter
                var employeesQuery = _unitOfWork.Employees.GetQueryable()
                    .Include(e => e.Department)
                    .Where(e => e.ComId == companyId.Value);

                if (departmentId.HasValue && departmentId != Guid.Empty)
                {
                    employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
                }

                var employees = await employeesQuery.OrderBy(e => e.EmpName).ToListAsync();

                // Get all attendances in date range for this company
                var attendances = await _unitOfWork.Attendances.GetQueryable()
                    .Include(a => a.Employee)
                    .Where(a => a.ComId == companyId.Value
                        && a.dtDate >= fromDateOnly
                        && a.dtDate <= toDateOnly)
                    .ToListAsync();

                // Calculate attendance counts per employee
                var attendanceCounts = attendances.GroupBy(a => a.EmpId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            Present = g.Count(a => a.AttStatus == "P"),
                            Late = g.Count(a => a.AttStatus == "L"),
                            AbsentRecorded = g.Count(a => a.AttStatus == "A")
                        }
                    );

                var result = employees.Select(e =>
                {
                    var counts = attendanceCounts.GetValueOrDefault(e.EmpId,
                        new { Present = 0, Late = 0, AbsentRecorded = 0 });

                    var attended = counts.Present + counts.Late;
                    var totalAbsent = totalDays - attended;

                    return new
                    {
                        employeeName = e.EmpName,  
                        totalPresent = attended,
                        totalAbsent = totalAbsent,
                        totalLate = counts.Late,
                        totalAbsentAgain = totalAbsent  
                    };
                }).ToList();

                return Json(new { success = true, data = result, totalCount = result.Count, totalDays = totalDays });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Download CSV
        [HttpGet]
        public async Task<IActionResult> DownloadAttendanceListCsv(Guid companyId, Guid? departmentId, DateTime fromDate, DateTime toDate)
        {
            var data = await GetAttendanceDataAsync(companyId, departmentId, fromDate, toDate);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
            await csv.WriteRecordsAsync(data);
            await writer.FlushAsync();
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "text/csv", "AttendanceList.csv");
        }

        // Download Excel
        [HttpGet]
        public async Task<IActionResult> DownloadAttendanceListExcel(Guid companyId, Guid? departmentId, DateTime fromDate, DateTime toDate)
        {
            var data = await GetAttendanceDataAsync(companyId, departmentId, fromDate, toDate);
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("AttendanceList");
            worksheet.Cells.LoadFromCollection(data, true);
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AttendanceList.xlsx");
        }

        // Download PDF
        [HttpGet]
        public async Task<IActionResult> DownloadAttendanceListPdf(Guid companyId, Guid? departmentId, DateTime fromDate, DateTime toDate)
        {
            var data = await GetAttendanceDataAsync(companyId, departmentId, fromDate, toDate);
            return new ViewAsPdf("AttendanceListPdf", data)
            {
                FileName = "AttendanceList.pdf",
                PageSize = Size.A4,
                PageMargins = new Margins(10, 10, 10, 10)
            };
        }

        private async Task<IEnumerable<object>> GetAttendanceDataAsync(Guid companyId, Guid? departmentId, DateTime fromDate, DateTime toDate)
        {
            var totalDays = (toDate - fromDate).Days + 1;

            var employeesQuery = _unitOfWork.Employees.GetQueryable()
                .Where(e => e.ComId == companyId);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();

            // Convert DateTime to DateOnly for comparison
            var fromDateOnly = DateOnly.FromDateTime(fromDate);
            var toDateOnly = DateOnly.FromDateTime(toDate);

            var attendances = await _unitOfWork.Attendances.GetQueryable()
                .Where(a => a.ComId == companyId && a.dtDate >= fromDateOnly && a.dtDate <= toDateOnly)
                .ToListAsync();

            var attendanceCounts = attendances.GroupBy(a => a.EmpId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Present = g.Count(a => a.AttStatus == "P"),
                        Late = g.Count(a => a.AttStatus == "L"),
                        AbsentRecorded = g.Count(a => a.AttStatus == "A")
                    }
                );

            return employees.Select(e =>
            {
                var counts = attendanceCounts.GetValueOrDefault(e.EmpId) ?? new { Present = 0, Late = 0, AbsentRecorded = 0 };
                var attended = counts.Present + counts.Late;
                var absent = counts.AbsentRecorded + (totalDays - (attended + counts.AbsentRecorded));
                return new
                {
                    EmployeeName = e.EmpName,
                    TotalPresent = attended,
                    TotalAbsent = absent,
                    TotalLate = counts.Late,
                    TotalAbsentAgain = absent  // Duplicated
                };
            });
        }

        // Third Report: Salary List
        public async Task<IActionResult> SalaryList()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            ViewBag.SelectedComId = selectedComId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalaryList(Guid? companyId, Guid? departmentId, int year, int month)
        {
            if (!companyId.HasValue) return BadRequest("Company required.");
            if (month < 1 || month > 12) return BadRequest("Invalid month.");

            var employeesQuery = _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Department)
                .Where(e => e.ComId == companyId.Value);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();

            var salaries = await _unitOfWork.Salaries.GetQueryable()
                .Where(s => s.ComId == companyId.Value && s.dtYear == year && s.dtMonth == month)
                .ToListAsync();

            var salaryDict = salaries.ToDictionary(s => s.EmpId);

            var result = employees.Select(e =>
            {
                var sal = salaryDict.GetValueOrDefault(e.EmpId);
                return new
                {
                    EmployeeName = e.EmpName,
                    TotalSalary = sal?.PayableAmount ?? 0,
                    TotalAbsentAmount = sal?.AbsentAmount ?? 0
                };
            });

            return Json(result);
        }

        // Download actions (CSV, Excel, PDF)
        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListCsv(Guid companyId, Guid? departmentId, int year, int month)
        {
            var data = await GetSalaryDataAsync(companyId, departmentId, year, month);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
            await csv.WriteRecordsAsync(data);
            await writer.FlushAsync();
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "text/csv", "SalaryList.csv");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListExcel(Guid companyId, Guid? departmentId, int year, int month)
        {
            var data = await GetSalaryDataAsync(companyId, departmentId, year, month);
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("SalaryList");
            worksheet.Cells.LoadFromCollection(data, true);
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalaryList.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListPdf(Guid companyId, Guid? departmentId, int year, int month)
        {
            var data = await GetSalaryDataAsync(companyId, departmentId, year, month);
            return new ViewAsPdf("SalaryListPdf", data)
            {
                FileName = "SalaryList.pdf",
                PageSize = Size.A4,
                PageMargins = new Margins(10, 10, 10, 10)
            };
        }

        private async Task<IEnumerable<object>> GetSalaryDataAsync(Guid companyId, Guid? departmentId, int year, int month)
        {
            var employeesQuery = _unitOfWork.Employees.GetQueryable()
                .Where(e => e.ComId == companyId);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();

            var salaries = await _unitOfWork.Salaries.GetQueryable()
                .Where(s => s.ComId == companyId && s.dtYear == year && s.dtMonth == month)
                .ToListAsync();

            var salaryDict = salaries.ToDictionary(s => s.EmpId);

            return employees.Select(e =>
            {
                var sal = salaryDict.GetValueOrDefault(e.EmpId);
                return new
                {
                    EmployeeName = e.EmpName,
                    TotalSalary = sal?.PayableAmount ?? 0,
                    TotalAbsentAmount = sal?.AbsentAmount ?? 0
                };
            });
        }

        // Fourth Report: Salary List Paid/Unpaid
        public async Task<IActionResult> SalaryListPaidUnpaid()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            ViewBag.Companies = companies;

            var selectedComId = Request.Cookies["SelectedComId"];
            ViewBag.SelectedComId = selectedComId;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalaryListPaidUnpaid(Guid? companyId, Guid? departmentId, int year, int month, bool? isPaid)
        {
            if (!companyId.HasValue) return BadRequest("Company required.");
            if (month < 1 || month > 12) return BadRequest("Invalid month.");

            var employeesQuery = _unitOfWork.Employees.GetQueryable()
                .Include(e => e.Department)
                .Where(e => e.ComId == companyId.Value);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();

            var salariesQuery = _unitOfWork.Salaries.GetQueryable()
                .Where(s => s.ComId == companyId.Value && s.dtYear == year && s.dtMonth == month);

            if (isPaid.HasValue)
            {
                salariesQuery = salariesQuery.Where(s => s.IsPaid == isPaid.Value);
            }

            var salaries = await salariesQuery.ToListAsync();

            var salaryDict = salaries.ToDictionary(s => s.EmpId);

            var result = employees.Select(e =>
            {
                var sal = salaryDict.GetValueOrDefault(e.EmpId);
                return new
                {
                    EmployeeName = e.EmpName,
                    Gross = sal?.Gross ?? 0,
                    Basic = sal?.Basic ?? 0,
                    HRent = sal?.HRent ?? 0,
                    Medical = sal?.Medical ?? 0,
                    AbsentAmount = sal?.AbsentAmount ?? 0,
                    PayableAmount = sal?.PayableAmount ?? 0
                };
            });

            return Json(result);
        }

        // Download actions (CSV, Excel, PDF)
        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListPaidUnpaidCsv(Guid companyId, Guid? departmentId, int year, int month, bool? isPaid)
        {
            var data = await GetSalaryPaidUnpaidDataAsync(companyId, departmentId, year, month, isPaid);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
            await csv.WriteRecordsAsync(data);
            await writer.FlushAsync();
            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "text/csv", "SalaryListPaidUnpaid.csv");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListPaidUnpaidExcel(Guid companyId, Guid? departmentId, int year, int month, bool? isPaid)
        {
            var data = await GetSalaryPaidUnpaidDataAsync(companyId, departmentId, year, month, isPaid);
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("SalaryListPaidUnpaid");
            worksheet.Cells.LoadFromCollection(data, true);
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalaryListPaidUnpaid.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSalaryListPaidUnpaidPdf(Guid companyId, Guid? departmentId, int year, int month, bool? isPaid)
        {
            var data = await GetSalaryPaidUnpaidDataAsync(companyId, departmentId, year, month, isPaid);
            return new ViewAsPdf("SalaryListPaidUnpaidPdf", data)
            {
                FileName = "SalaryListPaidUnpaid.pdf",
                PageSize = Size.A4,
                PageMargins = new Margins(10, 10, 10, 10)
            };
        }

        private async Task<IEnumerable<object>> GetSalaryPaidUnpaidDataAsync(Guid companyId, Guid? departmentId, int year, int month, bool? isPaid)
        {
            var employeesQuery = _unitOfWork.Employees.GetQueryable()
                .Where(e => e.ComId == companyId);

            if (departmentId.HasValue && departmentId != Guid.Empty)
            {
                employeesQuery = employeesQuery.Where(e => e.DeptId == departmentId.Value);
            }

            var employees = await employeesQuery.ToListAsync();

            var salariesQuery = _unitOfWork.Salaries.GetQueryable()
                .Where(s => s.ComId == companyId && s.dtYear == year && s.dtMonth == month);

            if (isPaid.HasValue)
            {
                salariesQuery = salariesQuery.Where(s => s.IsPaid == isPaid.Value);
            }

            var salaries = await salariesQuery.ToListAsync();

            var salaryDict = salaries.ToDictionary(s => s.EmpId);

            return employees.Select(e =>
            {
                var sal = salaryDict.GetValueOrDefault(e.EmpId);
                return new
                {
                    EmployeeName = e.EmpName,
                    Gross = sal?.Gross ?? 0,
                    Basic = sal?.Basic ?? 0,
                    HRent = sal?.HRent ?? 0,
                    Medical = sal?.Medical ?? 0,
                    AbsentAmount = sal?.AbsentAmount ?? 0,
                    PayableAmount = sal?.PayableAmount ?? 0
                };
            });
        }
    }
}