using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLeaveManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LeaveRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "LeaveRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId_Status",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedBy",
                table: "LeaveRequests",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_ApprovedBy",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_EmployeeId_Status",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_Status",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "LeaveRequests");
        }
    }
}
