using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PQM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumericColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ObjectType",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<double>(
                name: "NumericValue",
                table: "Register",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObisCode",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scaler",
                table: "Register",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ParameterValue",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Scaler",
                table: "Parameter",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Parameter",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NumericValue",
                table: "DeviceLog",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "DeviceLog",
                type: "nvarchar(max)",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "NumericValue",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "ObisCode",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "Scaler",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "Scaler",
                table: "Parameter");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Parameter");

            migrationBuilder.DropColumn(
                name: "NumericValue",
                table: "DeviceLog");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "DeviceLog");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Register",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ObjectType",
                table: "Register",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ParameterValue",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

        }
    }
}
