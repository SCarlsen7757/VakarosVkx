using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vakaros.Vkx.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "boat_classes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    length_over_all = table.Column<double>(type: "double precision", nullable: true),
                    beam = table.Column<double>(type: "double precision", nullable: true),
                    weight = table.Column<double>(type: "double precision", nullable: true),
                    bowsprit_length = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boat_classes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    active_from = table.Column<DateOnly>(type: "date", nullable: false),
                    active_until = table.Column<DateOnly>(type: "date", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "boats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    sail_number = table.Column<string>(type: "text", nullable: true),
                    boat_class_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boats", x => x.id);
                    table.ForeignKey(
                        name: "FK_boats_boat_classes_boat_class_id",
                        column: x => x.boat_class_id,
                        principalTable: "boat_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sails",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    boat_class_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    area = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sails", x => x.id);
                    table.ForeignKey(
                        name: "FK_sails_boat_classes_boat_class_id",
                        column: x => x.boat_class_id,
                        principalTable: "boat_classes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_legs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    mark_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    leg_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_legs", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_legs_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_legs_marks_mark_id",
                        column: x => x.mark_id,
                        principalTable: "marks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    boat_id = table.Column<int>(type: "integer", nullable: true),
                    course_id = table.Column<int>(type: "integer", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "text", nullable: false),
                    format_version = table.Column<short>(type: "smallint", nullable: false),
                    telemetry_rate_hz = table.Column<short>(type: "smallint", nullable: false),
                    is_fixed_to_body_frame = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessions_boats_boat_id",
                        column: x => x.boat_id,
                        principalTable: "boats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sessions_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "declinations",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    declination_offset = table.Column<float>(type: "real", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_declinations", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_declinations_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "depth_readings",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    depth = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_depth_readings", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_depth_readings_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "line_positions",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    line_end = table.Column<short>(type: "smallint", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_line_positions", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_line_positions_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "load_readings",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    sensor_name = table.Column<string>(type: "text", nullable: false),
                    load = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_load_readings", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_load_readings_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    speed_over_ground = table.Column<float>(type: "real", nullable: false),
                    course_over_ground = table.Column<float>(type: "real", nullable: false),
                    altitude = table.Column<float>(type: "real", nullable: false),
                    quaternion_w = table.Column<float>(type: "real", nullable: false),
                    quaternion_x = table.Column<float>(type: "real", nullable: false),
                    quaternion_y = table.Column<float>(type: "real", nullable: false),
                    quaternion_z = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_positions_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "race_timer_events",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    event_type = table.Column<short>(type: "smallint", nullable: false),
                    timer_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_race_timer_events", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_race_timer_events_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "races",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    race_number = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sailed_distance_meters = table.Column<double>(type: "double precision", nullable: false),
                    max_speed_over_ground = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_races", x => x.id);
                    table.ForeignKey(
                        name: "FK_races_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_angles",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    is_port = table.Column<bool>(type: "boolean", nullable: false),
                    is_manual = table.Column<bool>(type: "boolean", nullable: false),
                    true_heading = table.Column<float>(type: "real", nullable: false),
                    speed_over_ground = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_angles", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_shift_angles_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "speed_through_water",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    forward_speed = table.Column<float>(type: "real", nullable: false),
                    horizontal_speed = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speed_through_water", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_speed_through_water_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "temperature_readings",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temperature_readings", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_temperature_readings_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wind_readings",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    wind_direction = table.Column<float>(type: "real", nullable: false),
                    wind_speed = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wind_readings", x => new { x.time, x.session_id });
                    table.ForeignKey(
                        name: "FK_wind_readings_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_boats_boat_class_id",
                table: "boats",
                column: "boat_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_legs_course_id",
                table: "course_legs",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_legs_mark_id",
                table: "course_legs",
                column: "mark_id");

            migrationBuilder.CreateIndex(
                name: "IX_declinations_session_id",
                table: "declinations",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_depth_readings_session_id",
                table: "depth_readings",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_line_positions_session_id",
                table: "line_positions",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_load_readings_session_id",
                table: "load_readings",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_marks_name_active_from",
                table: "marks",
                columns: new[] { "name", "active_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_positions_session_id",
                table: "positions",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_race_timer_events_session_id",
                table: "race_timer_events",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_races_session_id_race_number",
                table: "races",
                columns: new[] { "session_id", "race_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sails_boat_class_id",
                table: "sails",
                column: "boat_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_boat_id",
                table: "sessions",
                column: "boat_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_content_hash",
                table: "sessions",
                column: "content_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_course_id",
                table: "sessions",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_shift_angles_session_id",
                table: "shift_angles",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_speed_through_water_session_id",
                table: "speed_through_water",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_temperature_readings_session_id",
                table: "temperature_readings",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_wind_readings_session_id",
                table: "wind_readings",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_legs");

            migrationBuilder.DropTable(
                name: "declinations");

            migrationBuilder.DropTable(
                name: "depth_readings");

            migrationBuilder.DropTable(
                name: "line_positions");

            migrationBuilder.DropTable(
                name: "load_readings");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "race_timer_events");

            migrationBuilder.DropTable(
                name: "races");

            migrationBuilder.DropTable(
                name: "sails");

            migrationBuilder.DropTable(
                name: "shift_angles");

            migrationBuilder.DropTable(
                name: "speed_through_water");

            migrationBuilder.DropTable(
                name: "temperature_readings");

            migrationBuilder.DropTable(
                name: "wind_readings");

            migrationBuilder.DropTable(
                name: "marks");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "boats");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "boat_classes");
        }
    }
}
