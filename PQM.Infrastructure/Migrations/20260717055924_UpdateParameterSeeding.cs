using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateParameterSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Accuracy Test Start", "0.128.162.0.128.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Accuracy Test Stop", "0.128.162.1.128.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Activity Calendar", "0.0.13.0.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Apparent Power – kVA", "1.0.9.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Association LN Meter Reader", "0.0.40.0.2.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Available Billing Periods", "0.0.0.1.1.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Billing Date", "0.0.0.1.2.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Billing Period Script Table", "0.0.10.0.1.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Capture Period of Daily Load Profile", "1.0.0.8.5.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Category", "0.0.94.91.11.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "CMRI Reset", "0.128.154.128.128.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "CT Rating", "0.0.94.91.12.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Billing Count", "0.0.0.1.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy – kVAh (Export)", "1.0.10.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy (kVAh)", "1.0.9.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy (kvarh) – Lag", "1.0.5.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy (kvarh) – Lead", "1.0.8.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy (kWh)", "1.0.1.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Energy (kWh) – Export", "1.0.2.8.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Power Failure Duration", "0.0.94.91.8.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Programming Count", "0.0.96.2.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Tamper Count", "0.0.94.91.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current – IB", "1.0.71.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current – IR", "1.0.31.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current – IY", "1.0.51.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current Related Event Code", "0.0.96.11.1.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Power Failure Related Event Code", "0.0.96.11.2.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 28,
                column: "Unit",
                value: null);

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "PT Power Fail Tamper Events", "1.0.128.7.90.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Reset Type", "0.128.153.128.128.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Signed Active Power – kW", "1.0.1.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Signed Power Factor – B Phase", "1.0.73.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Signed Power Factor – R Phase", "1.0.33.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Signed Power Factor – Y Phase", "1.0.53.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Signed Reactive Power – kvar", "1.0.3.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Single Action Schedule for Billing Dates", "0.0.15.0.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "TCP/UDP Setup", "0.0.25.0.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "TCP/UDP Setup IPv4 Address", "0.0.25.1.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "TCP/UDP Setup MAC Address", "0.0.25.2.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Transaction Related Event Code", "0.0.96.11.3.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Voltage – VBN", "1.0.72.7.0.255", null });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Voltage – VRN", "1.0.32.7.0.255", null });

            migrationBuilder.InsertData(
                table: "Parameter",
                columns: new[] { "Id", "Attribute3", "CreatedDate", "CreatedId", "IsActive", "IsDeleted", "ModifiedDate", "ModifiedId", "Name", "ObisCode", "ObjectType", "Scaler", "Unit" },
                values: new object[] { 43, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Voltage – VYN", "1.0.52.7.0.255", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Active Energy Import (kWh Import)", "1.0.1.8.0.255", "kWh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Active Energy Export (kWh Export)", "1.0.2.8.0.255", "kWh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Apparent Energy Import (kVAh Import)", "1.0.9.8.0.255", "kVAh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Apparent Energy Export (kVAh Export)", "1.0.10.8.0.255", "kVAh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Reactive Energy Lag (kvarh Lag)", "1.0.5.8.0.255", "kvarh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Reactive Energy Lead (kvarh Lead)", "1.0.8.8.0.255", "kvarh" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Voltage L1", "1.0.32.7.0.255", "Volt" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Voltage L2", "1.0.52.7.0.255", "Volt" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Voltage L3", "1.0.72.7.0.255", "Volt" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current L1", "1.0.31.7.0.255", "Amp" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current L2", "1.0.51.7.0.255", "Amp" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current L3", "1.0.71.7.0.255", "Amp" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Active Power", "1.0.1.7.0.255", "kW" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Reactive Power", "1.0.3.7.0.255", "kvar" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Apparent Power", "1.0.9.7.0.255", "kVA" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "L1 PF", "1.0.33.7.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "L2 PF", "1.0.53.7.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "L3 PF", "1.0.73.7.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Meter Category", "0.0.94.91.11.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "CT Rating", "0.0.94.91.12.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Activity Calendar", "0.0.13.0.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Billing Script Table", "0.0.10.0.1.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Billing Schedule", "0.0.15.0.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Association LN", "0.0.40.0.2.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "TCP/UDP Setup", "0.0.25.0.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "IPv4 Setup", "0.0.25.1.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "MAC Address", "0.0.25.2.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 28,
                column: "Unit",
                value: "");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Daily Capture Period", "1.0.0.8.5.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Billing Count", "0.0.0.1.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Current Related Event", "0.0.96.11.1.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Power Failure Event", "0.0.96.11.2.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Transaction Event", "0.0.96.11.3.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Tamper Count", "0.0.94.91.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "PT Power Fail Tamper Events", "1.0.128.7.90.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Cumulative Power Failure Duration", "0.0.94.91.8.255", "seconds" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Programming Count", "0.0.96.2.0.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Accuracy Test Start", "0.128.162.0.128.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Accuracy Test Stop", "0.128.162.1.128.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "Reset Type", "0.128.153.128.128.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "CMRI Reset", "0.128.154.128.128.255", "" });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "Name", "ObisCode", "Unit" },
                values: new object[] { "CMRI Reset (Manufacturer Specific)", "0.128.185.4.128.128.255", "" });
        }
    }
}
