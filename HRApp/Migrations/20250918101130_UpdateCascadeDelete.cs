using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAPP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_EmpId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmpId",
                table: "AttendanceSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Companies_ComId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Designations_Companies_ComId",
                table: "Designations");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Companies_ComId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DeptId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Designations_DesigId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Salaries_Employees_EmpId",
                table: "Salaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Companies_ComId",
                table: "Shifts");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmpId",
                table: "Attendances",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmpId",
                table: "AttendanceSummaries",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Companies_ComId",
                table: "Departments",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Designations_Companies_ComId",
                table: "Designations",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Companies_ComId",
                table: "Employees",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Departments_DeptId",
                table: "Employees",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Designations_DesigId",
                table: "Employees",
                column: "DesigId",
                principalTable: "Designations",
                principalColumn: "DesigId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Salaries_Employees_EmpId",
                table: "Salaries",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Companies_ComId",
                table: "Shifts",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Employees_EmpId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmpId",
                table: "AttendanceSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Companies_ComId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Designations_Companies_ComId",
                table: "Designations");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Companies_ComId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DeptId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Designations_DesigId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Salaries_Employees_EmpId",
                table: "Salaries");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Companies_ComId",
                table: "Shifts");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmpId",
                table: "Attendances",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSummaries_Employees_EmpId",
                table: "AttendanceSummaries",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Companies_ComId",
                table: "Departments",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Designations_Companies_ComId",
                table: "Designations",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Companies_ComId",
                table: "Employees",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Departments_DeptId",
                table: "Employees",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Designations_DesigId",
                table: "Employees",
                column: "DesigId",
                principalTable: "Designations",
                principalColumn: "DesigId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Shifts_ShiftId",
                table: "Employees",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "ShiftId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Salaries_Employees_EmpId",
                table: "Salaries",
                column: "EmpId",
                principalTable: "Employees",
                principalColumn: "EmpId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Companies_ComId",
                table: "Shifts",
                column: "ComId",
                principalTable: "Companies",
                principalColumn: "ComId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
