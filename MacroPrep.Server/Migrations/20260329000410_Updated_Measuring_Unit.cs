using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Measuring_Unit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Section",
                table: "RecipeImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SectionIndex",
                table: "RecipeImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "MeasuringUnits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Section",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "SectionIndex",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "MeasuringUnits");
        }
    }
}
