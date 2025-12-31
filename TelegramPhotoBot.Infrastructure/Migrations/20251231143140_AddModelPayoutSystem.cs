using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModelPayoutSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountStars = table.Column<long>(type: "bigint", nullable: false),
                    AmountFiat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelPayouts_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelPayouts_Users_ProcessedByAdminId",
                        column: x => x.ProcessedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_CompletedAt",
                table: "ModelPayouts",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_ModelId",
                table: "ModelPayouts",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_ModelId_Status",
                table: "ModelPayouts",
                columns: new[] { "ModelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_ProcessedByAdminId",
                table: "ModelPayouts",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_RequestedAt",
                table: "ModelPayouts",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelPayouts_Status",
                table: "ModelPayouts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelPayouts");
        }
    }
}
