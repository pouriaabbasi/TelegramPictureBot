using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModelTermsAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelTermsAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TermsVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TermsContent = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    IsLatestVersion = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelTermsAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelTermsAcceptances_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelTermsAcceptances_AcceptedAt",
                table: "ModelTermsAcceptances",
                column: "AcceptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTermsAcceptances_ModelId",
                table: "ModelTermsAcceptances",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTermsAcceptances_ModelId_IsLatestVersion",
                table: "ModelTermsAcceptances",
                columns: new[] { "ModelId", "IsLatestVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelTermsAcceptances_TermsVersion",
                table: "ModelTermsAcceptances",
                column: "TermsVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelTermsAcceptances");
        }
    }
}
