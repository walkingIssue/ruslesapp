using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rustles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipalityChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserCookiePolicyConsents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MunicipalityChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MunicipalityName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReplyToMessageId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicipalityChatMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MunicipalityChatMessages_MunicipalityChatMessages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "MunicipalityChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MunicipalityMessageReactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MunicipalityChatMessageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Emoji = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    ReactedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityMessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MunicipalityMessageReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MunicipalityMessageReactions_MunicipalityChatMessages_MunicipalityChatMessageId",
                        column: x => x.MunicipalityChatMessageId,
                        principalTable: "MunicipalityChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCookiePolicyConsents_UserId",
                table: "UserCookiePolicyConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityChatMessages_MunicipalityName_SentAt",
                table: "MunicipalityChatMessages",
                columns: new[] { "MunicipalityName", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityChatMessages_ReplyToMessageId",
                table: "MunicipalityChatMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityChatMessages_UserId",
                table: "MunicipalityChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityMessageReactions_MunicipalityChatMessageId_Emoji_ReactedAt",
                table: "MunicipalityMessageReactions",
                columns: new[] { "MunicipalityChatMessageId", "Emoji", "ReactedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityMessageReactions_UserId",
                table: "MunicipalityMessageReactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCookiePolicyConsents_AspNetUsers_UserId",
                table: "UserCookiePolicyConsents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCookiePolicyConsents_AspNetUsers_UserId",
                table: "UserCookiePolicyConsents");

            migrationBuilder.DropTable(
                name: "MunicipalityMessageReactions");

            migrationBuilder.DropTable(
                name: "MunicipalityChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_UserCookiePolicyConsents_UserId",
                table: "UserCookiePolicyConsents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserCookiePolicyConsents");
        }
    }
}
