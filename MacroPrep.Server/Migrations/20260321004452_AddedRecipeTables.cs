using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedRecipeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Servings",
                table: "Recipes",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float(18)");

            migrationBuilder.AlterColumn<float>(
                name: "RestingTimeMinutes",
                table: "Recipes",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float(18)");

            migrationBuilder.AlterColumn<float>(
                name: "PreparationTimeMinutes",
                table: "Recipes",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float(18)");

            migrationBuilder.AlterColumn<float>(
                name: "CookingTimeMinutes",
                table: "Recipes",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float(18)");

            migrationBuilder.AlterColumn<float>(
                name: "Ammount",
                table: "RecipeIngredients",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "float(18)");

            migrationBuilder.AlterColumn<float>(
                name: "Width",
                table: "RecipeImages",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "float(18)",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Height",
                table: "RecipeImages",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "float(18)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "Servings",
                table: "Recipes",
                type: "float(18)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "RestingTimeMinutes",
                table: "Recipes",
                type: "float(18)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "PreparationTimeMinutes",
                table: "Recipes",
                type: "float(18)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "CookingTimeMinutes",
                table: "Recipes",
                type: "float(18)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "Ammount",
                table: "RecipeIngredients",
                type: "float(18)",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "Width",
                table: "RecipeImages",
                type: "float(18)",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Height",
                table: "RecipeImages",
                type: "float(18)",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }
    }
}
