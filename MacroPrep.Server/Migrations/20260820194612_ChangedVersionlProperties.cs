using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MacroPrep.Server.Migrations
{
    /// <inheritdoc />
    public partial class ChangedVersionlProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VersionIsValid",
                table: "SemanticVersions",
                newName: "ForceRefresh");

            migrationBuilder.AddColumn<bool>(
                name: "ActiveVersion",
                table: "SemanticVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveVersion",
                table: "SemanticVersions");

            migrationBuilder.RenameColumn(
                name: "ForceRefresh",
                table: "SemanticVersions",
                newName: "VersionIsValid");
        }
    }
}
