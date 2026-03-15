using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletSessionUpgradeAddOnFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "WorkoutSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "MembershipId",
                table: "Invoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AddOnId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AddOnId",
                table: "Invoices",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_AddOns_BranchId",
                table: "AddOns",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_AddOns_AddOnId",
                table: "Invoices",
                column: "AddOnId",
                principalTable: "AddOns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_AddOns_AddOnId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "AddOns");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_AddOnId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "AddOnId",
                table: "Invoices");

            migrationBuilder.AlterColumn<int>(
                name: "MembershipId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Memberships_MembershipId",
                table: "Invoices",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
