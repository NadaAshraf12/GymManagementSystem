using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePasswordToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerMemberAssignments_MemberId",
                table: "TrainerMemberAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_MemberId",
                table: "TrainerMemberAssignments",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerMemberAssignments_MemberId",
                table: "TrainerMemberAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_MemberId",
                table: "TrainerMemberAssignments",
                column: "MemberId");
        }
    }
}
