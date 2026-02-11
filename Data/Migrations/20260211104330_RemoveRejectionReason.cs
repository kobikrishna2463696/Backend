using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrack.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PendingRegistrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "PendingRegistrations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
