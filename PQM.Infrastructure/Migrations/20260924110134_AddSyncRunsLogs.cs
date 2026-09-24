using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncRunsLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyncDeviceRuns_Devices_DeviceId",
                table: "SyncDeviceRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncDeviceRuns_SyncRuns_RunId",
                table: "SyncDeviceRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncRuns_DeviceSyncSchedule_ScheduleId",
                table: "SyncRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncRuns",
                table: "SyncRuns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncDeviceRuns",
                table: "SyncDeviceRuns");

            migrationBuilder.RenameTable(
                name: "SyncRuns",
                newName: "SyncRunLogs");

            migrationBuilder.RenameTable(
                name: "SyncDeviceRuns",
                newName: "SyncDeviceRunLogs");

            migrationBuilder.RenameIndex(
                name: "IX_SyncRuns_ScheduleId",
                table: "SyncRunLogs",
                newName: "IX_SyncRunLogs_ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncDeviceRuns_RunId",
                table: "SyncDeviceRunLogs",
                newName: "IX_SyncDeviceRunLogs_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncDeviceRuns_DeviceId",
                table: "SyncDeviceRunLogs",
                newName: "IX_SyncDeviceRunLogs_DeviceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncRunLogs",
                table: "SyncRunLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncDeviceRunLogs",
                table: "SyncDeviceRunLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SyncDeviceRunLogs_Devices_DeviceId",
                table: "SyncDeviceRunLogs",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncDeviceRunLogs_SyncRunLogs_RunId",
                table: "SyncDeviceRunLogs",
                column: "RunId",
                principalTable: "SyncRunLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncRunLogs_DeviceSyncSchedule_ScheduleId",
                table: "SyncRunLogs",
                column: "ScheduleId",
                principalTable: "DeviceSyncSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyncDeviceRunLogs_Devices_DeviceId",
                table: "SyncDeviceRunLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncDeviceRunLogs_SyncRunLogs_RunId",
                table: "SyncDeviceRunLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncRunLogs_DeviceSyncSchedule_ScheduleId",
                table: "SyncRunLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncRunLogs",
                table: "SyncRunLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncDeviceRunLogs",
                table: "SyncDeviceRunLogs");

            migrationBuilder.RenameTable(
                name: "SyncRunLogs",
                newName: "SyncRuns");

            migrationBuilder.RenameTable(
                name: "SyncDeviceRunLogs",
                newName: "SyncDeviceRuns");

            migrationBuilder.RenameIndex(
                name: "IX_SyncRunLogs_ScheduleId",
                table: "SyncRuns",
                newName: "IX_SyncRuns_ScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncDeviceRunLogs_RunId",
                table: "SyncDeviceRuns",
                newName: "IX_SyncDeviceRuns_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncDeviceRunLogs_DeviceId",
                table: "SyncDeviceRuns",
                newName: "IX_SyncDeviceRuns_DeviceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncRuns",
                table: "SyncRuns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncDeviceRuns",
                table: "SyncDeviceRuns",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SyncDeviceRuns_Devices_DeviceId",
                table: "SyncDeviceRuns",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncDeviceRuns_SyncRuns_RunId",
                table: "SyncDeviceRuns",
                column: "RunId",
                principalTable: "SyncRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncRuns_DeviceSyncSchedule_ScheduleId",
                table: "SyncRuns",
                column: "ScheduleId",
                principalTable: "DeviceSyncSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
