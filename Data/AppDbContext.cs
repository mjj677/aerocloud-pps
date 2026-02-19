using AeroCloud.PPS.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroCloud.PPS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<BagDrop> BagDrops => Set<BagDrop>();
    public DbSet<Flight> Flights => Set<Flight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Passenger>(e =>
        {
            e.HasIndex(p => p.BookingReference).IsUnique();
            e.Property(p => p.CheckInStatus).HasConversion<string>();
            // SQL Server: explicit column type for decimal precision
            e.Property(p => p.CreatedAt).HasColumnType("datetime2");
            e.Property(p => p.UpdatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<BagDrop>(e =>
        {
            e.HasIndex(b => b.BagTagNumber).IsUnique();
            e.Property(b => b.Status).HasConversion<string>();
            e.Property(b => b.WeightKg).HasColumnType("decimal(5,2)");
            e.Property(b => b.RegisteredAt).HasColumnType("datetime2");
            e.HasOne(b => b.Passenger)
             .WithMany(p => p.Bags)
             .HasForeignKey(b => b.PassengerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Flight>(e =>
        {
            e.HasIndex(f => f.FlightNumber);
            e.Property(f => f.Status).HasConversion<string>();
            e.Property(f => f.ScheduledDeparture).HasColumnType("datetime2");
        });

        // Seed data — static dates required for EF migration snapshots
        var seedDate = new DateTime(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Flight>().HasData(
            new Flight
            {
                Id = 1,
                FlightNumber = "EZY1234",
                Origin = "MAN",
                Destination = "AMS",
                ScheduledDeparture = seedDate.AddHours(3),
                Status = FlightStatus.Boarding,
                Gate = "B14"
            },
            new Flight
            {
                Id = 2,
                FlightNumber = "BA0456",
                Origin = "MAN",
                Destination = "LHR",
                ScheduledDeparture = seedDate.AddHours(6),
                Status = FlightStatus.Scheduled,
                Gate = "A07"
            }
        );

        modelBuilder.Entity<Passenger>().HasData(
            new Passenger
            {
                Id = 1,
                FullName = "Jane Smith",
                BookingReference = "ABC123",
                FlightNumber = "EZY1234",
                SeatNumber = "14A",
                CheckInStatus = CheckInStatus.CheckedIn,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}
