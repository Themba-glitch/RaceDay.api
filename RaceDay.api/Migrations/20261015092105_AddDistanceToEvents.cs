using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDistanceToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKM",
                table: "Events",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKM",
                table: "Events");
        }
    }
}
