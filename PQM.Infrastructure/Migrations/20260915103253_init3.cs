using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ReadingValues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ReadingValues");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "ReadTime",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DeviceProfileSyncState");

            migrationBuilder.DropColumn(
                name: "LastReadTimestampUtc",
                table: "DeviceProfileSyncState");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ReadingSessions",
                newName: "ReadTimeAt");

            migrationBuilder.RenameColumn(
                name: "NextRunAtUtc",
                table: "DeviceSyncSchedule",
                newName: "NextRunAt");

            migrationBuilder.RenameColumn(
                name: "LastRunAtUtc",
                table: "DeviceSyncSchedule",
                newName: "LastRunAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "DeviceProfileSyncState",
                newName: "LastReadTimestamp");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "DeviceProfileSyncState",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReadTimeAt",
                table: "ReadingSessions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "NextRunAt",
                table: "DeviceSyncSchedule",
                newName: "NextRunAtUtc");

            migrationBuilder.RenameColumn(
                name: "LastRunAt",
                table: "DeviceSyncSchedule",
                newName: "LastRunAtUtc");

            migrationBuilder.RenameColumn(
                name: "LastReadTimestamp",
                table: "DeviceProfileSyncState",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ReadingValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ReadingValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ReadingSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadTime",
                table: "ReadingSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "DeviceProfileSyncState",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DeviceProfileSyncState",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadTimestampUtc",
                table: "DeviceProfileSyncState",
                type: "datetime2",
                nullable: true);
        }
    }
}
