using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthProject.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingTotpSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingTotpSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingTotpSecret",
                table: "Users");
        }
    }
}
