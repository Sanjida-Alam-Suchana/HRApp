using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryComponentsToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Basic",
                table: "Employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HRent",
                table: "Employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Medical",
                table: "Employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Others",
                table: "Employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Basic",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HRent",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Medical",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Others",
                table: "Employees");
        }
    }
}
