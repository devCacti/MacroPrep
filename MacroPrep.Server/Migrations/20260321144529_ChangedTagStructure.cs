using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangedTagStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Ingredients_IngredientId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Users_UserId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_IngredientId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_UserId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IngredientId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tags");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                table: "Recipes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IngredientTag",
                columns: table => new
                {
                    IngredientsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientTag", x => new { x.IngredientsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_IngredientTag_Ingredients_IngredientsId",
                        column: x => x.IngredientsId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagUser",
                columns: table => new
                {
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagUser", x => new { x.TagsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TagUser_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_TagId",
                table: "Recipes",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientTag_TagsId",
                table: "IngredientTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_TagUser_UsersId",
                table: "TagUser",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Tags_TagId",
                table: "Recipes",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Tags_TagId",
                table: "Recipes");

            migrationBuilder.DropTable(
                name: "IngredientTag");

            migrationBuilder.DropTable(
                name: "TagUser");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_TagId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "Recipes");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "IngredientId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IngredientId",
                table: "Tags",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Ingredients_IngredientId",
                table: "Tags",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Users_UserId",
                table: "Tags",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
