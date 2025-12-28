using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMtProtoCachedPhotoInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MtProtoAccessHash",
                table: "Photos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "MtProtoFileReference",
                table: "Photos",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MtProtoPhotoId",
                table: "Photos",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MtProtoAccessHash",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MtProtoFileReference",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MtProtoPhotoId",
                table: "Photos");
        }
    }
}
