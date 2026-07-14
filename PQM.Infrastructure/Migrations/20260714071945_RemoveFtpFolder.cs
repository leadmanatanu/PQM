using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFtpFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FtpFolder",
                table: "Device");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FtpFolder",
                table: "Device",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
