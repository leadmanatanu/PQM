using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMeterTypeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionSchedule");

            migrationBuilder.DropTable(
                name: "AssociationMetadata");

            migrationBuilder.DropTable(
                name: "CalendarData");

            migrationBuilder.DropTable(
                name: "CommunicationValue");

            migrationBuilder.DropTable(
                name: "DeviceEvent");

            migrationBuilder.DropTable(
                name: "DeviceLog");

            migrationBuilder.DropTable(
                name: "DLMSObject");

            migrationBuilder.DropTable(
                name: "ProfileCaptureObject");

            migrationBuilder.DropTable(
                name: "ProfileData");

            migrationBuilder.DropTable(
                name: "ProfileGeneric");

            migrationBuilder.DropTable(
                name: "RegisterValue");

            migrationBuilder.DropTable(
                name: "ScriptAction");

            migrationBuilder.DropTable(
                name: "ScriptTable");

            migrationBuilder.DropIndex(
                name: "IX_Parameter_ObisCode",
                table: "Parameter");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Parameter");

            migrationBuilder.AlterColumn<string>(
                name: "ObisCode",
                table: "Parameter",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeName",
                table: "Parameter",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TypeName",
                table: "Device",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 1,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 2,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 3,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 4,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 5,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 6,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 7,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 8,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 9,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 10,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 11,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 12,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 13,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 14,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 15,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 16,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 17,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 18,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 19,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 20,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 21,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 22,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 23,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 24,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 25,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 26,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 27,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 28,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 29,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 30,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 31,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 32,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 33,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 34,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 35,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 36,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 37,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 38,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 39,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 40,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 41,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 42,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 43,
                column: "TypeName",
                value: "ABT");

            migrationBuilder.InsertData(
                table: "Parameter",
                columns: new[] { "Id", "Attribute3", "CreatedDate", "CreatedId", "IsActive", "IsDeleted", "ModifiedDate", "ModifiedId", "Name", "ObisCode", "ObjectType", "Scaler", "TypeName", "Unit" },
                values: new object[,]
                {
                    { 44, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Voltage L1", "1.0.32.7.0.251", null, null, "ABT", null },
                    { 45, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Voltage L2", "1.0.52.7.0.251", null, null, "ABT", null },
                    { 46, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Current L1", "1.0.31.7.0.251", null, null, "ABT", null },
                    { 47, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Active Power", "1.0.1.7.0.251", null, null, "ABT", null },
                    { 48, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Billing Energy", "1.0.9.8.0.251", null, null, "ABT", null },
                    { 49, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Import Energy", "1.0.1.8.0.251", null, null, "ABT", null },
                    { 50, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Export Energy", "1.0.2.8.0.251", null, null, "ABT", null },
                    { 51, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, null, null, "Maximum Demand", "1.0.9.6.0.251", null, null, "ABT", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DropColumn(
                name: "TypeName",
                table: "Parameter");

            migrationBuilder.DropColumn(
                name: "TypeName",
                table: "Device");

            migrationBuilder.AlterColumn<string>(
                name: "ObisCode",
                table: "Parameter",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Parameter",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActionSchedule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ExecutionTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScriptName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Selector = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionSchedule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssociationMetadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssociationStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthenticationMechanism = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientSAP = table.Column<int>(type: "int", nullable: false),
                    Conformance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    DlmsVersion = table.Column<int>(type: "int", nullable: false),
                    MaxReceivePduSize = table.Column<int>(type: "int", nullable: false),
                    MaxSendPduSize = table.Column<int>(type: "int", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SecuritySetupReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServerSAP = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarNameActive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalendarNamePassive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayProfileActive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayProfilePassive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeasonProfileActive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeasonProfilePassive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeekProfileActive = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeekProfilePassive = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationValue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    FormattedValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    EventCode = table.Column<int>(type: "int", nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    RawClock = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DLMSObject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeAccessRights = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    LastReadTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MethodAccessRights = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DLMSObject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileCaptureObject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeIndex = table.Column<int>(type: "int", nullable: false),
                    CaptureObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataIndex = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ProfileGenericId = table.Column<long>(type: "bigint", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileCaptureObject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ProfileGenericId = table.Column<long>(type: "bigint", nullable: false),
                    RawDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileGeneric",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapturePeriod = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    LastReadTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileGeneric", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegisterValue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    DisplayValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ParsedValue = table.Column<double>(type: "float", nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Scaler = table.Column<int>(type: "int", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisterValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScriptAction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    ParameterValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptId = table.Column<int>(type: "int", nullable: false),
                    ScriptTableId = table.Column<long>(type: "bigint", nullable: false),
                    TargetIndex = table.Column<int>(type: "int", nullable: false),
                    TargetObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptAction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScriptTable",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ObisCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParameterId = table.Column<int>(type: "int", nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScriptObisCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptTable", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 1,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 2,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 3,
                column: "Category",
                value: "Calendar");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 4,
                column: "Category",
                value: "Power");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 5,
                column: "Category",
                value: "Association");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 6,
                column: "Category",
                value: "Calendar");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 7,
                column: "Category",
                value: "Calendar");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 8,
                column: "Category",
                value: "Script");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 9,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 10,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 11,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 12,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 13,
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 14,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 15,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 16,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 17,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 18,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 19,
                column: "Category",
                value: "Energy");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 20,
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 21,
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 22,
                column: "Category",
                value: "General");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 23,
                column: "Category",
                value: "Current");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 24,
                column: "Category",
                value: "Current");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 25,
                column: "Category",
                value: "Current");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 26,
                column: "Category",
                value: "Event");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 27,
                column: "Category",
                value: "Event");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 28,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 29,
                column: "Category",
                value: "Event");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 30,
                column: "Category",
                value: "Configuration");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 31,
                column: "Category",
                value: "Power");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 32,
                column: "Category",
                value: "PowerFactor");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 33,
                column: "Category",
                value: "PowerFactor");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 34,
                column: "Category",
                value: "PowerFactor");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 35,
                column: "Category",
                value: "Power");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 36,
                column: "Category",
                value: "Schedule");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 37,
                column: "Category",
                value: "Communication");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 38,
                column: "Category",
                value: "Communication");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 39,
                column: "Category",
                value: "Communication");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 40,
                column: "Category",
                value: "Event");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 41,
                column: "Category",
                value: "Voltage");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 42,
                column: "Category",
                value: "Voltage");

            migrationBuilder.UpdateData(
                table: "Parameter",
                keyColumn: "Id",
                keyValue: 43,
                column: "Category",
                value: "Voltage");

            migrationBuilder.CreateIndex(
                name: "IX_Parameter_ObisCode",
                table: "Parameter",
                column: "ObisCode",
                unique: true,
                filter: "[ObisCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActionSchedule_DeviceId_ObisCode",
                table: "ActionSchedule",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationMetadata_DeviceId_ObisCode",
                table: "AssociationMetadata",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarData_DeviceId_ObisCode",
                table: "CalendarData",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationValue_DeviceId_ObisCode",
                table: "CommunicationValue",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DLMSObject_DeviceId_ObisCode",
                table: "DLMSObject",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCaptureObject_ProfileGenericId_DataIndex",
                table: "ProfileCaptureObject",
                columns: new[] { "ProfileGenericId", "DataIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileGeneric_DeviceId_ObisCode",
                table: "ProfileGeneric",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScriptTable_DeviceId_ObisCode",
                table: "ScriptTable",
                columns: new[] { "DeviceId", "ObisCode" },
                unique: true);
        }
    }
}
