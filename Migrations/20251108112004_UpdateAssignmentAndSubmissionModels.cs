using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualClassroom2.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssignmentAndSubmissionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Submissions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Submissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAt",
                table: "Submissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNotes",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Classrooms",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentFilePath",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssignmentFileSize",
                table: "Assignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentFileType",
                table: "Assignments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "StudentNotes",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "AssignmentFilePath",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AssignmentFileSize",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AssignmentFileType",
                table: "Assignments");

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Classrooms",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
