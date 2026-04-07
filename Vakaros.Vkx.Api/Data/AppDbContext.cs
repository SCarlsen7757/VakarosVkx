using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Models.Entities;

namespace Vakaros.Vkx.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Relational tables
    public DbSet<Boat> Boats => Set<Boat>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseLeg> CourseLegs => Set<CourseLeg>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Race> Races => Set<Race>();

    // Hypertables
    public DbSet<PositionReading> Positions => Set<PositionReading>();
    public DbSet<WindReading> WindReadings => Set<WindReading>();
    public DbSet<SpeedThroughWaterReading> SpeedThroughWater => Set<SpeedThroughWaterReading>();
    public DbSet<DepthReading> DepthReadings => Set<DepthReading>();
    public DbSet<TemperatureReading> TemperatureReadings => Set<TemperatureReading>();
    public DbSet<LoadReading> LoadReadings => Set<LoadReading>();
    public DbSet<DeclinationReading> Declinations => Set<DeclinationReading>();
    public DbSet<RaceTimerEvent> RaceTimerEvents => Set<RaceTimerEvent>();
    public DbSet<LinePositionReading> LinePositions => Set<LinePositionReading>();
    public DbSet<ShiftAngleReading> ShiftAngles => Set<ShiftAngleReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Boats ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Boat>(e =>
        {
            e.ToTable("boats");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.Name).HasColumnName("name").IsRequired();
            e.Property(b => b.SailNumber).HasColumnName("sail_number");
            e.Property(b => b.BoatClass).HasColumnName("boat_class");
            e.Property(b => b.Description).HasColumnName("description");
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
        });

        // ── Marks ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Mark>(e =>
        {
            e.ToTable("marks");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.Name).HasColumnName("name").IsRequired();
            e.Property(m => m.Year).HasColumnName("year");
            e.Property(m => m.Latitude).HasColumnName("latitude");
            e.Property(m => m.Longitude).HasColumnName("longitude");
            e.Property(m => m.Description).HasColumnName("description");
            e.HasIndex(m => new { m.Name, m.Year }).IsUnique();
        });

        // ── Courses ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").IsRequired();
            e.Property(c => c.Year).HasColumnName("year");
            e.Property(c => c.Description).HasColumnName("description");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<CourseLeg>(e =>
        {
            e.ToTable("course_legs");
            e.HasKey(cl => cl.Id);
            e.Property(cl => cl.Id).HasColumnName("id");
            e.Property(cl => cl.CourseId).HasColumnName("course_id");
            e.Property(cl => cl.MarkId).HasColumnName("mark_id");
            e.Property(cl => cl.SortOrder).HasColumnName("sort_order");
            e.Property(cl => cl.LegName).HasColumnName("leg_name");
            e.HasOne(cl => cl.Course).WithMany(c => c.Legs).HasForeignKey(cl => cl.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cl => cl.Mark).WithMany(m => m.CourseLegs).HasForeignKey(cl => cl.MarkId);
        });

        // ── Sessions ────────────────────────────────────────────────────────
        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasColumnName("id");
            e.Property(s => s.BoatId).HasColumnName("boat_id");
            e.Property(s => s.CourseId).HasColumnName("course_id");
            e.Property(s => s.FileName).HasColumnName("file_name").IsRequired();
            e.Property(s => s.ContentHash).HasColumnName("content_hash").IsRequired();
            e.HasIndex(s => s.ContentHash).IsUnique();
            e.Property(s => s.FormatVersion).HasColumnName("format_version");
            e.Property(s => s.TelemetryRateHz).HasColumnName("telemetry_rate_hz");
            e.Property(s => s.IsFixedToBodyFrame).HasColumnName("is_fixed_to_body_frame");
            e.Property(s => s.StartedAt).HasColumnName("started_at");
            e.Property(s => s.EndedAt).HasColumnName("ended_at");
            e.Property(s => s.UploadedAt).HasColumnName("uploaded_at");
            e.Property(s => s.Notes).HasColumnName("notes");
            e.HasOne(s => s.Boat).WithMany(b => b.Sessions).HasForeignKey(s => s.BoatId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Course).WithMany(c => c.Sessions).HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Races ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Race>(e =>
        {
            e.ToTable("races");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.SessionId).HasColumnName("session_id");
            e.Property(r => r.RaceNumber).HasColumnName("race_number");
            e.Property(r => r.StartedAt).HasColumnName("started_at");
            e.Property(r => r.EndedAt).HasColumnName("ended_at");
            e.HasIndex(r => new { r.SessionId, r.RaceNumber }).IsUnique();
            e.HasOne(r => r.Session).WithMany(s => s.Races).HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Hypertables ─────────────────────────────────────────────────────
        // TimescaleDB hypertables have no traditional PK; use (time, session_id) composite.

        modelBuilder.Entity<PositionReading>(e =>
        {
            e.ToTable("positions");
            e.HasKey(p => new { p.Time, p.SessionId });
            e.Property(p => p.Time).HasColumnName("time");
            e.Property(p => p.SessionId).HasColumnName("session_id");
            e.Property(p => p.Latitude).HasColumnName("latitude");
            e.Property(p => p.Longitude).HasColumnName("longitude");
            e.Property(p => p.SpeedOverGround).HasColumnName("speed_over_ground");
            e.Property(p => p.CourseOverGround).HasColumnName("course_over_ground");
            e.Property(p => p.Altitude).HasColumnName("altitude");
            e.Property(p => p.QuaternionW).HasColumnName("quaternion_w");
            e.Property(p => p.QuaternionX).HasColumnName("quaternion_x");
            e.Property(p => p.QuaternionY).HasColumnName("quaternion_y");
            e.Property(p => p.QuaternionZ).HasColumnName("quaternion_z");
            e.HasOne(p => p.Session).WithMany().HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WindReading>(e =>
        {
            e.ToTable("wind_readings");
            e.HasKey(w => new { w.Time, w.SessionId });
            e.Property(w => w.Time).HasColumnName("time");
            e.Property(w => w.SessionId).HasColumnName("session_id");
            e.Property(w => w.WindDirection).HasColumnName("wind_direction");
            e.Property(w => w.WindSpeed).HasColumnName("wind_speed");
            e.HasOne(w => w.Session).WithMany().HasForeignKey(w => w.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpeedThroughWaterReading>(e =>
        {
            e.ToTable("speed_through_water");
            e.HasKey(s => new { s.Time, s.SessionId });
            e.Property(s => s.Time).HasColumnName("time");
            e.Property(s => s.SessionId).HasColumnName("session_id");
            e.Property(s => s.ForwardSpeed).HasColumnName("forward_speed");
            e.Property(s => s.HorizontalSpeed).HasColumnName("horizontal_speed");
            e.HasOne(s => s.Session).WithMany().HasForeignKey(s => s.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DepthReading>(e =>
        {
            e.ToTable("depth_readings");
            e.HasKey(d => new { d.Time, d.SessionId });
            e.Property(d => d.Time).HasColumnName("time");
            e.Property(d => d.SessionId).HasColumnName("session_id");
            e.Property(d => d.Depth).HasColumnName("depth");
            e.HasOne(d => d.Session).WithMany().HasForeignKey(d => d.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemperatureReading>(e =>
        {
            e.ToTable("temperature_readings");
            e.HasKey(t => new { t.Time, t.SessionId });
            e.Property(t => t.Time).HasColumnName("time");
            e.Property(t => t.SessionId).HasColumnName("session_id");
            e.Property(t => t.Temperature).HasColumnName("temperature");
            e.HasOne(t => t.Session).WithMany().HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoadReading>(e =>
        {
            e.ToTable("load_readings");
            e.HasKey(l => new { l.Time, l.SessionId });
            e.Property(l => l.Time).HasColumnName("time");
            e.Property(l => l.SessionId).HasColumnName("session_id");
            e.Property(l => l.SensorName).HasColumnName("sensor_name");
            e.Property(l => l.Load).HasColumnName("load");
            e.HasOne(l => l.Session).WithMany().HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeclinationReading>(e =>
        {
            e.ToTable("declinations");
            e.HasKey(d => new { d.Time, d.SessionId });
            e.Property(d => d.Time).HasColumnName("time");
            e.Property(d => d.SessionId).HasColumnName("session_id");
            e.Property(d => d.DeclinationOffset).HasColumnName("declination_offset");
            e.Property(d => d.Latitude).HasColumnName("latitude");
            e.Property(d => d.Longitude).HasColumnName("longitude");
            e.HasOne(d => d.Session).WithMany().HasForeignKey(d => d.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RaceTimerEvent>(e =>
        {
            e.ToTable("race_timer_events");
            e.HasKey(r => new { r.Time, r.SessionId });
            e.Property(r => r.Time).HasColumnName("time");
            e.Property(r => r.SessionId).HasColumnName("session_id");
            e.Property(r => r.EventType).HasColumnName("event_type");
            e.Property(r => r.TimerValue).HasColumnName("timer_value");
            e.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LinePositionReading>(e =>
        {
            e.ToTable("line_positions");
            e.HasKey(l => new { l.Time, l.SessionId });
            e.Property(l => l.Time).HasColumnName("time");
            e.Property(l => l.SessionId).HasColumnName("session_id");
            e.Property(l => l.LineEnd).HasColumnName("line_end");
            e.Property(l => l.Latitude).HasColumnName("latitude");
            e.Property(l => l.Longitude).HasColumnName("longitude");
            e.HasOne(l => l.Session).WithMany().HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShiftAngleReading>(e =>
        {
            e.ToTable("shift_angles");
            e.HasKey(s => new { s.Time, s.SessionId });
            e.Property(s => s.Time).HasColumnName("time");
            e.Property(s => s.SessionId).HasColumnName("session_id");
            e.Property(s => s.IsPort).HasColumnName("is_port");
            e.Property(s => s.IsManual).HasColumnName("is_manual");
            e.Property(s => s.TrueHeading).HasColumnName("true_heading");
            e.Property(s => s.SpeedOverGround).HasColumnName("speed_over_ground");
            e.HasOne(s => s.Session).WithMany().HasForeignKey(s => s.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
