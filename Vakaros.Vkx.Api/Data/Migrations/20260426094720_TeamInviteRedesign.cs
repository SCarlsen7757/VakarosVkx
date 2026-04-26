using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakaros.Vkx.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeamInviteRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_invites_team_id_email",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_token",
                table: "team_invites");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "team_invites",
                newName: "role");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "declined_at",
                table: "team_invites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "invited_user_id",
                table: "team_invites",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_invited_user_id",
                table: "team_invites",
                column: "invited_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_team_id_invited_user_id",
                table: "team_invites",
                columns: new[] { "team_id", "invited_user_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_team_invites_users_invited_user_id",
                table: "team_invites",
                column: "invited_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_invites_users_invited_user_id",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_invited_user_id",
                table: "team_invites");

            migrationBuilder.DropIndex(
                name: "IX_team_invites_team_id_invited_user_id",
                table: "team_invites");

            migrationBuilder.DropColumn(
                name: "declined_at",
                table: "team_invites");

            migrationBuilder.DropColumn(
                name: "invited_user_id",
                table: "team_invites");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "team_invites",
                newName: "token");

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_team_id_email",
                table: "team_invites",
                columns: new[] { "team_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_team_invites_token",
                table: "team_invites",
                column: "token",
                unique: true);
        }
    }
}
