using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDeviceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authentication",
                table: "Device",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientAddress",
                table: "Device",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Device",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerAddress",
                table: "Device",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Timeout",
                table: "Device",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authentication",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "ClientAddress",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "ServerAddress",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "Timeout",
                table: "Device");
        }
    }
}
