using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameEntryTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingSessions_Device_Profile_Timestamp",
                table: "ReadingSessions");

            migrationBuilder.RenameColumn(
                name: "EntryTimestampUtc",
                table: "ReadingSessions",
                newName: "EntryTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_Device_Profile_Timestamp",
                table: "ReadingSessions",
                columns: new[] { "DeviceId", "ProfileId", "EntryTimestamp" },
                unique: true,
                filter: "[EntryTimestamp] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingSessions_Device_Profile_Timestamp",
                table: "ReadingSessions");

            migrationBuilder.RenameColumn(
                name: "EntryTimestamp",
                table: "ReadingSessions",
                newName: "EntryTimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_Device_Profile_Timestamp",
                table: "ReadingSessions",
                columns: new[] { "DeviceId", "ProfileId", "EntryTimestampUtc" },
                unique: true,
                filter: "[EntryTimestampUtc] IS NOT NULL");
        }
    }
}
