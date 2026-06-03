using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriCasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationStatusToWeeklyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "generation_status",
                table: "weekly_plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "generation_status",
                table: "weekly_plans");
        }
    }
}
