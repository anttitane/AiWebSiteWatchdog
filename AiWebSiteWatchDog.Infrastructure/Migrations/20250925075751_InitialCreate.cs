using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiWebSiteWatchDog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    SenderEmail = table.Column<string>(type: "TEXT", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.SenderEmail);
                });

            migrationBuilder.CreateTable(
                name: "GoogleOAuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleOAuthTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WatchTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    InterestSentence = table.Column<string>(type: "TEXT", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastResult = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    GeminiApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    WatchUrl = table.Column<string>(type: "TEXT", nullable: false),
                    InterestSentence = table.Column<string>(type: "TEXT", nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", nullable: false),
                    EmailSettingsSenderEmail = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Email);
                    table.ForeignKey(
                        name: "FK_UserSettings_EmailSettings_EmailSettingsSenderEmail",
                        column: x => x.EmailSettingsSenderEmail,
                        principalTable: "EmailSettings",
                        principalColumn: "SenderEmail",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleOAuthTokens_Email",
                table: "GoogleOAuthTokens",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_EmailSettingsSenderEmail",
                table: "UserSettings",
                column: "EmailSettingsSenderEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleOAuthTokens");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "WatchTasks");

            migrationBuilder.DropTable(
                name: "EmailSettings");
        }
    }
}
