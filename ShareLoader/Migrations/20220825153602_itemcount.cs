using Microsoft.EntityFrameworkCore.Migrations;

namespace ShareLoader.Migrations
{
    public partial class itemcount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Groups",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "ItemsCount",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsCount",
                table: "Groups");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Groups",
                newName: "ID");
        }
    }
}
