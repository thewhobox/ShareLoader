using Microsoft.EntityFrameworkCore.Migrations;

namespace ShareLoader.Migrations
{
    public partial class Items : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Groups_DownloadGroupID",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_DownloadGroupID",
                table: "Items");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_DownloadGroupID",
                table: "Items",
                column: "DownloadGroupID");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Groups_DownloadGroupID",
                table: "Items",
                column: "DownloadGroupID",
                principalTable: "Groups",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
