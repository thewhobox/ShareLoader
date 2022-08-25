using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ShareLoader.Migrations
{
    public partial class account : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    TrafficLeft = table.Column<float>(type: "REAL", nullable: false),
                    TrafficLeftWeek = table.Column<float>(type: "REAL", nullable: false),
                    IsPremium = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidTill = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Credit = table.Column<float>(type: "REAL", nullable: false),
                    Hoster = table.Column<string>(type: "TEXT", nullable: false),
                    AllowClientRedirect = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
