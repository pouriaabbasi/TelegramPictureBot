using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBotTokenFromPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove bot token from PlatformSettings if it exists
            // Bot token must remain in appsettings.json for bootstrapping
            migrationBuilder.Sql(
                @"DELETE FROM [PlatformSettings] 
                  WHERE [Key] = 'telegram:bot_token' AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No action needed on rollback - bot token should remain in appsettings.json
        }
    }
}
