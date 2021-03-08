using Microsoft.EntityFrameworkCore.Migrations;

namespace ShareLoader.Migrations
{
    public partial class AllowRedirect : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowClientRedirect",
                table: "Accounts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowClientRedirect",
                table: "Accounts");
        }
    }
}
