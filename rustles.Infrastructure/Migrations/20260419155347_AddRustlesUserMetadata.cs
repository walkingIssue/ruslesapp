using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rustles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRustlesUserMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAgeSelfAttestations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAgeSelfAttestations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAgeSelfAttestations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCookiePolicyConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCookiePolicyConsents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAgeSelfAttestations_UserId",
                table: "UserAgeSelfAttestations",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAgeSelfAttestations");

            migrationBuilder.DropTable(
                name: "UserCookiePolicyConsents");
        }
    }
}
