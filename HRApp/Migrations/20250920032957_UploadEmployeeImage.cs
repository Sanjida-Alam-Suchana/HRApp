using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAPP.Migrations
{
    /// <inheritdoc />
    public partial class UploadEmployeeImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeImage",
                table: "Employees",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeImage",
                table: "Employees");
        }
    }
}
