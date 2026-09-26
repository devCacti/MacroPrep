using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemovedPasswordSaltField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_RecipeIngredients_RecipeIngredientId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_RecipeIngredientId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecipeIngredientId",
                table: "Tags");

            migrationBuilder.CreateTable(
                name: "RecipeIngredientTag",
                columns: table => new
                {
                    RecipeIngredientsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredientTag", x => new { x.RecipeIngredientsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_RecipeIngredientTag_RecipeIngredients_RecipeIngredientsId",
                        column: x => x.RecipeIngredientsId,
                        principalTable: "RecipeIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeIngredientTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListMembers_UserId",
                table: "ListMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientTag_TagsId",
                table: "RecipeIngredientTag",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ListMembers_Users_UserId",
                table: "ListMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListMembers_Users_UserId",
                table: "ListMembers");

            migrationBuilder.DropTable(
                name: "RecipeIngredientTag");

            migrationBuilder.DropIndex(
                name: "IX_ListMembers_UserId",
                table: "ListMembers");

            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RecipeIngredientId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_RecipeIngredientId",
                table: "Tags",
                column: "RecipeIngredientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_RecipeIngredients_RecipeIngredientId",
                table: "Tags",
                column: "RecipeIngredientId",
                principalTable: "RecipeIngredients",
                principalColumn: "Id");
        }
    }
}
