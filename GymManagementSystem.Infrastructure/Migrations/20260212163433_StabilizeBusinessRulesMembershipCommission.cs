using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StabilizeBusinessRulesMembershipCommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Commissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Commissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_BranchId",
                table: "Commissions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_MembershipId_Source",
                table: "Commissions",
                columns: new[] { "MembershipId", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commissions_BranchId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_MembershipId_Source",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Commissions");
        }
    }
}
