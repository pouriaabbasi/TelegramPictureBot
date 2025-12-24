using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramPhotoBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchasePhotos");

            migrationBuilder.DropTable(
                name: "PurchaseSubscriptions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.AddColumn<Guid>(
                name: "ModelId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Purchases",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Purchases",
                type: "bit",
                nullable: true,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModelId",
                table: "Purchases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PhotoId",
                table: "Purchases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseType",
                table: "Purchases",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "Purchases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionPeriod_EndDate",
                table: "Purchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionPeriod_StartDate",
                table: "Purchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Purchases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModelId",
                table: "Photos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DemoImage_FileId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DemoImage_FileUniqueId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DemoImage_FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DemoImage_MimeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DemoImage_FileSize = table.Column<long>(type: "bigint", nullable: true),
                    DemoImage_Width = table.Column<int>(type: "int", nullable: true),
                    DemoImage_Height = table.Column<int>(type: "int", nullable: true),
                    SubscriptionPrice = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionDurationDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalSubscribers = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalContentItems = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePhoto_PhotoId",
                table: "Purchases",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ModelId",
                table: "Purchases",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId_ModelId_IsActive",
                table: "Purchases",
                columns: new[] { "UserId", "ModelId", "IsActive" },
                filter: "[IsDeleted] = 0 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_UserId1",
                table: "Purchases",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSubscription_SubscriptionId",
                table: "Purchases",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ModelId",
                table: "Photos",
                column: "ModelId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ModelId_Type",
                table: "Photos",
                columns: new[] { "ModelId", "Type" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Models_DisplayName",
                table: "Models",
                column: "DisplayName",
                filter: "[IsDeleted] = 0 AND [Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Models_Status",
                table: "Models",
                column: "Status",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Models_UserId",
                table: "Models",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Models_ModelId",
                table: "Photos",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Models_ModelId",
                table: "Purchases",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Photos_PhotoId",
                table: "Purchases",
                column: "PhotoId",
                principalTable: "Photos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Subscriptions_SubscriptionId",
                table: "Purchases",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Users_UserId1",
                table: "Purchases",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Models_ModelId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Models_ModelId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Photos_PhotoId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Subscriptions_SubscriptionId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Users_UserId1",
                table: "Purchases");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePhoto_PhotoId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ModelId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_UserId_ModelId_IsActive",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_UserId1",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseSubscription_SubscriptionId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Photos_ModelId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_ModelId_Type",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PhotoId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PurchaseType",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SubscriptionPeriod_EndDate",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SubscriptionPeriod_StartDate",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Photos");

            migrationBuilder.CreateTable(
                name: "PurchasePhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhotoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchasePhotos_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchasePhotos_Purchases_Id",
                        column: x => x.Id,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseSubscriptions_Purchases_Id",
                        column: x => x.Id,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseSubscriptions_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePhoto_PhotoId",
                table: "PurchasePhotos",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseSubscription_SubscriptionId",
                table: "PurchaseSubscriptions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_User_Role",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);
        }
    }
}
