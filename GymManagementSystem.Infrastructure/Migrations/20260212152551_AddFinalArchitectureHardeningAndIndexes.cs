using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalArchitectureHardeningAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_MembershipId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MemberId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_MembershipId",
                table: "Commissions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ReferenceId",
                table: "WalletTransactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Type_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MembershipId_PaymentStatus_CreatedAt",
                table: "Payments",
                columns: new[] { "MembershipId", "PaymentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidAt",
                table: "Payments",
                column: "PaidAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MemberId_CreatedAt",
                table: "Invoices",
                columns: new[] { "MemberId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_MembershipId_CreatedAt",
                table: "Commissions",
                columns: new[] { "MembershipId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_PaidAt",
                table: "Commissions",
                column: "PaidAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_ReferenceId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_Type_CreatedAt",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MembershipId_PaymentStatus_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaidAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MemberId_CreatedAt",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_MembershipId_CreatedAt",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_PaidAt",
                table: "Commissions");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MembershipId",
                table: "Payments",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MemberId",
                table: "Invoices",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_MembershipId",
                table: "Commissions",
                column: "MembershipId");
        }
    }
}
