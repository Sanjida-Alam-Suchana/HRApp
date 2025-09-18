using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddPayableAmountToSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PayableAmount",
                table: "Salaries",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayableAmount",
                table: "Salaries");
        }

    }
}
