using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiWebSiteWatchDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationChannelAndTelegram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationChannel",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TelegramBotToken",
                table: "UserSettings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramChatId",
                table: "UserSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationChannel",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "TelegramBotToken",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                table: "UserSettings");
        }
    }
}
