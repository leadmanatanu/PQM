using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStatusMappingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ObisCode",
                table: "Register",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EventStatusMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BitIndex = table.Column<int>(type: "int", nullable: false),
                    EventCode = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStatusMapping", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EventStatusMapping",
                columns: new[] { "Id", "BitIndex", "Category", "EventCode", "Label", "ObisCode" },
                values: new object[,]
                {
                    { 1, 0, "voltage", 1, "R-Phase - Voltage Missing - Occurrence", "0.0.96.11.0.255" },
                    { 2, 1, "voltage", 2, "R-Phase - Voltage Missing - Restoration", "0.0.96.11.0.255" },
                    { 3, 2, "voltage", 3, "Y-Phase - Voltage Missing - Occurrence", "0.0.96.11.0.255" },
                    { 4, 3, "voltage", 4, "Y-Phase - Voltage Missing - Restoration", "0.0.96.11.0.255" },
                    { 5, 4, "voltage", 5, "B-Phase - Voltage Missing - Occurrence", "0.0.96.11.0.255" },
                    { 6, 5, "voltage", 6, "B-Phase - Voltage Missing - Restoration", "0.0.96.11.0.255" },
                    { 7, 6, "voltage", 7, "Over Voltage in any Phase - Occurrence", "0.0.96.11.0.255" },
                    { 8, 7, "voltage", 8, "Over Voltage in any Phase - Restoration", "0.0.96.11.0.255" },
                    { 9, 8, "voltage", 9, "Low Voltage in any Phase - Occurrence", "0.0.96.11.0.255" },
                    { 10, 9, "voltage", 10, "Low Voltage in any Phase - Restoration", "0.0.96.11.0.255" },
                    { 11, 10, "voltage", 11, "Voltage Unbalance - Occurrence", "0.0.96.11.0.255" },
                    { 12, 11, "voltage", 12, "Voltage Unbalance - Restoration", "0.0.96.11.0.255" },
                    { 13, 0, "current", 51, "R Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 14, 1, "current", 52, "R Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 15, 2, "current", 53, "Y Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 16, 3, "current", 54, "Y Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 17, 4, "current", 55, "B Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 18, 5, "current", 56, "B Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 19, 6, "current", 63, "Current Unbalance - Occurrence", "0.0.96.11.1.255" },
                    { 20, 7, "current", 64, "Current Unbalance - Restoration", "0.0.96.11.1.255" },
                    { 21, 8, "current", 65, "Current bypass - Occurrence", "0.0.96.11.1.255" },
                    { 22, 9, "current", 66, "Current bypass - Restoration", "0.0.96.11.1.255" },
                    { 23, 10, "current", 67, "Over current in any phase - Occurrence", "0.0.96.11.1.255" },
                    { 24, 11, "current", 68, "Over current in any phase - Restoration", "0.0.96.11.1.255" },
                    { 25, 0, "power", 101, "Power failure - Occurrence", "0.0.96.11.2.255" },
                    { 26, 1, "power", 102, "Power failure - Restoration", "0.0.96.11.2.255" },
                    { 27, 0, "transaction", 151, "Real Time Clock - Date and Time", "0.0.96.11.3.255" },
                    { 28, 1, "transaction", 152, "Demand Integration Period", "0.0.96.11.3.255" },
                    { 29, 2, "transaction", 153, "Profile Capture Period", "0.0.96.11.3.255" },
                    { 30, 3, "transaction", 154, "Single-action Schedule for Billing Dates", "0.0.96.11.3.255" },
                    { 31, 4, "transaction", 155, "Activity Calendar Time Zones", "0.0.96.11.3.255" },
                    { 32, 5, "transaction", 157, "New Firmware Activated", "0.0.96.11.3.255" },
                    { 33, 6, "transaction", 158, "Load limit (kW) set", "0.0.96.11.3.255" },
                    { 34, 7, "transaction", 159, "Enabled - load limit function", "0.0.96.11.3.255" },
                    { 35, 8, "transaction", 160, "Disabled - load limit function", "0.0.96.11.3.255" },
                    { 36, 9, "transaction", 161, "LLS secret (MR) change", "0.0.96.11.3.255" },
                    { 37, 10, "transaction", 162, "HLS key (US) change", "0.0.96.11.3.255" },
                    { 38, 11, "transaction", 163, "HLS key (FW) change", "0.0.96.11.3.255" },
                    { 39, 12, "transaction", 164, "Global key change(encryption and authentication)", "0.0.96.11.3.255" },
                    { 40, 13, "transaction", 165, "ESWF change", "0.0.96.11.3.255" },
                    { 41, 14, "transaction", 166, "MD reset", "0.0.96.11.3.255" },
                    { 42, 15, "transaction", 169, "Single Action Schedule for Image Activation", "0.0.96.11.3.255" },
                    { 43, 16, "transaction", 182, "Passive Relay time.", "0.0.96.11.3.255" },
                    { 44, 0, "others", 201, "Influence of permanent magnet - Occurrence", "0.0.96.11.4.255" },
                    { 45, 1, "others", 202, "Influence of permanent magnet - Restoration", "0.0.96.11.4.255" },
                    { 46, 2, "others", 203, "Neutral disturbance - Occurrence", "0.0.96.11.4.255" },
                    { 47, 3, "others", 204, "Neutral disturbance - Restoration", "0.0.96.11.4.255" },
                    { 48, 4, "others", 205, "Meter cover opened", "0.0.96.11.4.255" },
                    { 50, 5, "others", 206, "Terminal cover opened", "0.0.96.11.4.255" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventStatusMapping");

            migrationBuilder.AlterColumn<string>(
                name: "ObisCode",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
