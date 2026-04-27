using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vakaros.Vkx.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceIdToRaceSummaryReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "race_id",
                table: "race_summary_reports",
                type: "uuid",
                nullable: true,
                defaultValue: null);

            // Backfill race_id from the races table using session_id + race_number.
            migrationBuilder.Sql("""
                UPDATE race_summary_reports rsr
                SET race_id = r.id
                FROM races r
                WHERE r.session_id = rsr.session_id
                  AND r.race_number = rsr.race_number;
                """);

            // Delete orphaned summaries where no matching race was found.
            migrationBuilder.Sql("DELETE FROM race_summary_reports WHERE race_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "race_id",
                table: "race_summary_reports",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_race_summary_reports_race_id",
                table: "race_summary_reports",
                column: "race_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_race_summary_reports_races_race_id",
                table: "race_summary_reports",
                column: "race_id",
                principalTable: "races",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_race_summary_reports_races_race_id",
                table: "race_summary_reports");

            migrationBuilder.DropIndex(
                name: "IX_race_summary_reports_race_id",
                table: "race_summary_reports");

            migrationBuilder.DropColumn(
                name: "race_id",
                table: "race_summary_reports");
        }
    }
}
