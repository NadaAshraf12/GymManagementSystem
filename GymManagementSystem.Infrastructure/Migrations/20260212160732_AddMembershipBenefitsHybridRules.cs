using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipBenefitsHybridRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberSessions_MemberId",
                table: "MemberSessions");

            migrationBuilder.AddColumn<bool>(
                name: "AddOnAccess",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "IncludedSessionsPerMonth",
                table: "MembershipPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PriorityBooking",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SessionDiscountPercentage",
                table: "MembershipPlans",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedDiscountPercentage",
                table: "MemberSessions",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargedPrice",
                table: "MemberSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "MemberSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PriorityBookingApplied",
                table: "MemberSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UsedIncludedSession",
                table: "MemberSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresActiveMembership",
                table: "AddOns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MemberSessions_MemberId_BookingDate_UsedIncludedSession",
                table: "MemberSessions",
                columns: new[] { "MemberId", "BookingDate", "UsedIncludedSession" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberSessions_MemberId_BookingDate_UsedIncludedSession",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "AddOnAccess",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IncludedSessionsPerMonth",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "PriorityBooking",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "SessionDiscountPercentage",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountPercentage",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "ChargedPrice",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "PriorityBookingApplied",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "UsedIncludedSession",
                table: "MemberSessions");

            migrationBuilder.DropColumn(
                name: "RequiresActiveMembership",
                table: "AddOns");

            migrationBuilder.CreateIndex(
                name: "IX_MemberSessions_MemberId",
                table: "MemberSessions",
                column: "MemberId");
        }
    }
}
