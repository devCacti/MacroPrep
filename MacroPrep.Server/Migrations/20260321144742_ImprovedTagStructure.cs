using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class ImprovedTagStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Tags_TagId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_TagId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "Recipes");

            migrationBuilder.CreateTable(
                name: "RecipeTag",
                columns: table => new
                {
                    RecipesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeTag", x => new { x.RecipesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_RecipeTag_Recipes_RecipesId",
                        column: x => x.RecipesId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTag_TagsId",
                table: "RecipeTag",
                column: "TagsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeTag");

            migrationBuilder.AddColumn<Guid>(
                name: "TagId",
                table: "Recipes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_TagId",
                table: "Recipes",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Tags_TagId",
                table: "Recipes",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id");
        }
    }
}
