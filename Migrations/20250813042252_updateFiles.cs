using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WEBAPI_m1IL_1.Migrations
{
    /// <inheritdoc />
    public partial class updateFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "description",
                table: "DocumentationFiles",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "DocumentationFiles",
                newName: "description");
        }
    }
}
