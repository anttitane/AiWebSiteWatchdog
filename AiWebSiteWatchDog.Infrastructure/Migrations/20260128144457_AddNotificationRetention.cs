using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiWebSiteWatchDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationRetentionDays",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationRetentionDays",
                table: "UserSettings");
        }
    }
}
