using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class SemanticVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Details",
                table: "Recipes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "ListItems",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "Instruments",
                newName: "Description");

            migrationBuilder.CreateTable(
                name: "SemanticVersions",
                columns: table => new
                {
                    VersionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Major = table.Column<int>(type: "int", nullable: false),
                    Minor = table.Column<int>(type: "int", nullable: false),
                    Patch = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VersionIsValid = table.Column<bool>(type: "bit", nullable: false),
                    Component = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticVersions", x => x.VersionID);
                    table.ForeignKey(
                        name: "FK_SemanticVersions_Users_CreatedByUserID",
                        column: x => x.CreatedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemanticVersions_Users_UpdatedByUserID",
                        column: x => x.UpdatedByUserID,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SemanticVersions_CreatedByUserID",
                table: "SemanticVersions",
                column: "CreatedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_SemanticVersions_UpdatedByUserID",
                table: "SemanticVersions",
                column: "UpdatedByUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SemanticVersions");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Recipes",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ListItems",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Instruments",
                newName: "Details");
        }
    }
}
