using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseHardeningControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WalletTransactions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProofUrl",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreezeEndDate",
                table: "Memberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreezeStartDate",
                table: "Memberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Memberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Memberships",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AspNetUsers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPlans_Name_IsDeleted",
                table: "MembershipPlans",
                columns: new[] { "Name", "IsDeleted" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Member_WalletBalance_NonNegative",
                table: "AspNetUsers",
                sql: "[WalletBalance] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipPlans_Name_IsDeleted",
                table: "MembershipPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Member_WalletBalance_NonNegative",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentProofUrl",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FreezeEndDate",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "FreezeStartDate",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AspNetUsers");
        }
    }
}
