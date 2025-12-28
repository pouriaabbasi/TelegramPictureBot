using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMtProtoLastMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MtProtoLastMessageId",
                table: "Photos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MtProtoLastMessageId",
                table: "Photos");
        }
    }
}
