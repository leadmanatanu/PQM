using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanAndSimplifySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedHeader");

            migrationBuilder.DropTable(
                name: "DeviceLog");

            migrationBuilder.DropTable(
                name: "DeviceParameterMapping");

            migrationBuilder.DropTable(
                name: "DLMSObject");

            migrationBuilder.DropTable(
                name: "EventLog");

            migrationBuilder.DropTable(
                name: "EventStatusMapping");

            migrationBuilder.DropTable(
                name: "FTPSetting");

            migrationBuilder.DropTable(
                name: "ObjectParameter");

            migrationBuilder.DropTable(
                name: "ProfileGenericEntry");

            migrationBuilder.DropTable(
                name: "Register");

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "ParameterValue",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Device",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "ParameterValue");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Device");

            migrationBuilder.CreateTable(
                name: "ActionSchedule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSchedule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityCalendar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCalendar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssociationLogicalName",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationLogicalName", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clock",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedHeader",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedHeader", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Data",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    NumericValue = table.Column<double>(type: "float", nullable: true),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceParameterMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateStamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceParameterMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DLMSObject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeaderId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DLMSObject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    A = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    B = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    C = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<double>(type: "float", nullable: true),
                    End_Time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Max_Voltage = table.Column<double>(type: "float", nullable: true),
                    Min_Voltage = table.Column<double>(type: "float", nullable: true),
                    Phase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Start_Time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UMAX = table.Column<double>(type: "float", nullable: true),
                    USS = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventStatusMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BitIndex = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventCode = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStatusMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FTPSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FtpHost = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootFolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTPSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IecHdlcSetup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IecHdlcSetup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ip4Setup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ip4Setup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MacAddressSetup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacAddressSetup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectParameter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileGeneric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileGeneric", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileGenericEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumericValue = table.Column<double>(type: "float", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileGenericEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Register",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumericValue = table.Column<double>(type: "float", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scaler = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Register", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScriptTable",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptTable", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TcpUdpSetup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateEntered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TcpUdpSetup", x => x.Id);
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
                    { 13, 4, "current", 51, "R Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 14, 5, "current", 52, "R Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 15, 8, "current", 53, "Y Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 16, 9, "current", 54, "Y Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 17, 10, "current", 55, "B Phase - Current reverse - Occurrence", "0.0.96.11.1.255" },
                    { 18, 11, "current", 56, "B Phase - Current reverse - Restoration", "0.0.96.11.1.255" },
                    { 19, 7, "current", 63, "Current Unbalance - Occurrence", "0.0.96.11.1.255" },
                    { 20, 6, "current", 64, "Current Unbalance - Restoration", "0.0.96.11.1.255" },
                    { 21, 0, "current", 65, "Current bypass - Occurrence", "0.0.96.11.1.255" },
                    { 22, 1, "current", 66, "Current bypass - Restoration", "0.0.96.11.1.255" },
                    { 23, 2, "current", 67, "Over current in any phase - Occurrence", "0.0.96.11.1.255" },
                    { 24, 3, "current", 68, "Over current in any phase - Restoration", "0.0.96.11.1.255" },
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
    }
}
