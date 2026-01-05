using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingStarPaymentInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingStarPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    ContentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<int>(type: "int", nullable: false),
                    RequiredStars = table.Column<int>(type: "int", nullable: false),
                    ReceivedStars = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PaymentMessageId = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingStarPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingStarPayments_Photos_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingStarPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingStarPayments_ContentId",
                table: "PendingStarPayments",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingStarPayments_ExpiresAt",
                table: "PendingStarPayments",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingStarPayments_PaymentMessageId_ChatId",
                table: "PendingStarPayments",
                columns: new[] { "PaymentMessageId", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingStarPayments_Status",
                table: "PendingStarPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingStarPayments_UserId",
                table: "PendingStarPayments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingStarPayments");
        }
    }
}
