using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rustles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipalityModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "MunicipalityChatMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "MunicipalityChatMessages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "MunicipalityChatMessages",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MunicipalityUserBans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MunicipalityName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    BannedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityUserBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicipalityUserBans_AspNetUsers_BannedByUserId",
                        column: x => x.BannedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityUserBans_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityUserBans_BannedByUserId",
                table: "MunicipalityUserBans",
                column: "BannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityUserBans_MunicipalityName_UserId_ExpiresAt",
                table: "MunicipalityUserBans",
                columns: new[] { "MunicipalityName", "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityUserBans_UserId",
                table: "MunicipalityUserBans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MunicipalityUserBans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MunicipalityChatMessages");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MunicipalityChatMessages");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "MunicipalityChatMessages");
        }
    }
}
