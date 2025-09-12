using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComName = table.Column<string>(type: "text", nullable: false),
                    Basic = table.Column<decimal>(type: "numeric", nullable: false),
                    HRent = table.Column<decimal>(type: "numeric", nullable: false),
                    Medical = table.Column<decimal>(type: "numeric", nullable: false),
                    IsInactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeptName = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Designations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesigName = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Designations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftName = table.Column<string>(type: "text", nullable: false),
                    In = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Out = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Late = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmpName = table.Column<string>(type: "text", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeptId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesigId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    Gross = table.Column<decimal>(type: "numeric", nullable: false),
                    Basic = table.Column<decimal>(type: "numeric", nullable: false),
                    HRent = table.Column<decimal>(type: "numeric", nullable: false),
                    Medical = table.Column<decimal>(type: "numeric", nullable: false),
                    Others = table.Column<decimal>(type: "numeric", nullable: false),
                    dtJoin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Designations_DesignationId",
                        column: x => x.DesignationId,
                        principalTable: "Designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    dtDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AttStatus = table.Column<string>(type: "text", nullable: false),
                    InTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    OutTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    dtYear = table.Column<int>(type: "integer", nullable: false),
                    dtMonth = table.Column<int>(type: "integer", nullable: false),
                    Present = table.Column<int>(type: "integer", nullable: false),
                    Late = table.Column<int>(type: "integer", nullable: false),
                    Absent = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_Companies_ComId",
                        column: x => x.ComId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_Employees_EmpId",
                        column: x => x.EmpId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpId = table.Column<Guid>(type: "uuid", nullable: false),
                    dtYear = table.Column<int>(type: "integer", nullable: false),
                    dtMonth = table.Column<int>(type: "integer", nullable: false),
                    Gross = table.Column<decimal>(type: "numeric", nullable: false),
                    Basic = table.Column<decimal>(type: "numeric", nullable: false),
                    HRent = table.Column<decimal>(type: "numeric", nullable: false),
                    Medical = table.Column<decimal>(type: "numeric", nullable: false),
                    AbsentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PayableAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Salaries_Companies_ComId",
                        column: x => x.ComId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Salaries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Salaries_Employees_EmpId",
                        column: x => x.EmpId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Salaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CompanyId",
                table: "Attendances",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId",
                table: "Attendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_ComId",
                table: "AttendanceSummaries",
                column: "ComId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_CompanyId",
                table: "AttendanceSummaries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmpId",
                table: "AttendanceSummaries",
                column: "EmpId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmployeeId",
                table: "AttendanceSummaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Designations_CompanyId",
                table: "Designations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DesignationId",
                table: "Employees",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ShiftId",
                table: "Employees",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_ComId",
                table: "Salaries",
                column: "ComId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_CompanyId",
                table: "Salaries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_EmpId",
                table: "Salaries",
                column: "EmpId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_EmployeeId",
                table: "Salaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CompanyId",
                table: "Shifts",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "AttendanceSummaries");

            migrationBuilder.DropTable(
                name: "Salaries");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Designations");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
