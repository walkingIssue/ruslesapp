using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rustles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformPolicyAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolicyKey = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPolicyAcceptances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PlatformPolicyId = table.Column<int>(type: "INTEGER", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicyAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPolicyAcceptances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPolicyAcceptances_PlatformPolicies_PlatformPolicyId",
                        column: x => x.PlatformPolicyId,
                        principalTable: "PlatformPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPolicies_PolicyKey_Version",
                table: "PlatformPolicies",
                columns: new[] { "PolicyKey", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAcceptances_PlatformPolicyId",
                table: "UserPolicyAcceptances",
                column: "PlatformPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicyAcceptances_UserId_PlatformPolicyId",
                table: "UserPolicyAcceptances",
                columns: new[] { "UserId", "PlatformPolicyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPolicyAcceptances");

            migrationBuilder.DropTable(
                name: "PlatformPolicies");
        }
    }
}
