using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventStatusMappingSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ObisCode",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 13,
                column: "BitIndex",
                value: 4);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 14,
                column: "BitIndex",
                value: 5);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 15,
                column: "BitIndex",
                value: 8);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 16,
                column: "BitIndex",
                value: 9);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 17,
                column: "BitIndex",
                value: 10);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 18,
                column: "BitIndex",
                value: 11);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 19,
                column: "BitIndex",
                value: 7);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 20,
                column: "BitIndex",
                value: 6);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 21,
                column: "BitIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 22,
                column: "BitIndex",
                value: 1);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 23,
                column: "BitIndex",
                value: 2);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 24,
                column: "BitIndex",
                value: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 13,
                column: "BitIndex",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 14,
                column: "BitIndex",
                value: 1);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 15,
                column: "BitIndex",
                value: 2);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 16,
                column: "BitIndex",
                value: 3);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 17,
                column: "BitIndex",
                value: 4);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 18,
                column: "BitIndex",
                value: 5);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 19,
                column: "BitIndex",
                value: 6);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 20,
                column: "BitIndex",
                value: 7);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 21,
                column: "BitIndex",
                value: 8);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 22,
                column: "BitIndex",
                value: 9);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 23,
                column: "BitIndex",
                value: 10);

            migrationBuilder.UpdateData(
                table: "EventStatusMapping",
                keyColumn: "Id",
                keyValue: 24,
                column: "BitIndex",
                value: 11);
        }
    }
}
