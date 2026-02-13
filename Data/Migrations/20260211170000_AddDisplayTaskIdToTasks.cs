using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrack.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayTaskIdToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayTaskId",
                table: "Tasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE Tasks 
                SET DisplayTaskId = 'TASK-' + RIGHT('000' + CAST(TaskId AS VARCHAR), 3)
                WHERE DisplayTaskId = '' OR DisplayTaskId IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayTaskId",
                table: "Tasks");
        }
    }
}
