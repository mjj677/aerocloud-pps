using AeroCloud.PPS.Data;
using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroCloud.PPS.Services;

public class FlightService : IFlightService
{
    private readonly AppDbContext _db;

    public FlightService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<FlightResponse>> GetAllAsync()
    {
        var flights = await _db.Flights.AsNoTracking().ToListAsync();

        var passengerCounts = await _db.Passengers
            .AsNoTracking()
            .GroupBy(p => p.FlightNumber)
            .Select(g => new { FlightNumber = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FlightNumber, x => x.Count);

        return flights.Select(f => new FlightResponse(
            f.Id,
            f.FlightNumber,
            f.Origin,
            f.Destination,
            f.ScheduledDeparture,
            f.Status.ToString(),
            f.Gate,
            passengerCounts.TryGetValue(f.FlightNumber, out var count) ? count : 0
        ));
    }

    public async Task<FlightResponse?> GetByFlightNumberAsync(string flightNumber)
    {
        var flight = await _db.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber.ToUpperInvariant());

        if (flight is null) return null;

        var passengerCount = await _db.Passengers
            .CountAsync(p => p.FlightNumber == flightNumber.ToUpperInvariant());

        return new FlightResponse(
            flight.Id,
            flight.FlightNumber,
            flight.Origin,
            flight.Destination,
            flight.ScheduledDeparture,
            flight.Status.ToString(),
            flight.Gate,
            passengerCount
        );
    }

    /// <summary>
    /// Builds the flight manifest using LINQ to:
    ///  - Filter passengers to only those on this flight
    ///  - Include each passenger's bags via eager loading (.Include)
    ///  - Order by seat row (numeric part) then column (letter) using
    ///    a compound OrderBy / ThenBy
    ///  - Project into ManifestEntry records using Select
    /// </summary>
    public async Task<IEnumerable<ManifestEntry>?> GetManifestAsync(string flightNumber)
    {
        var fn = flightNumber.ToUpperInvariant();

        var flightExists = await _db.Flights.AnyAsync(f => f.FlightNumber == fn);
        if (!flightExists) return null;

        // Load passengers + bags from DB first (simple SQL-translatable query)
        var passengers = await _db.Passengers
            .AsNoTracking()
            .Include(p => p.Bags)
            .Where(p => p.FlightNumber == fn)
            .ToListAsync();

        // Sort in-memory — seat number parsing can't be translated to SQL
        var sorted = passengers
            .OrderBy(p => p.SeatNumber == null)
            .ThenBy(p => p.SeatNumber == null
                ? 0
                : int.Parse(new string(p.SeatNumber.TakeWhile(char.IsDigit).ToArray())))
            .ThenBy(p => p.SeatNumber)
            .ThenBy(p => p.FullName);

        return sorted.Select(p => new ManifestEntry(
            p.FullName,
            p.BookingReference,
            p.SeatNumber,
            p.CheckInStatus.ToString(),
            p.Bags.Select(b => new BagDropResponse(b.Id, b.BagTagNumber, b.WeightKg, b.Status.ToString(), b.RegisteredAt))
        ));
    }

    /// <summary>
    /// Computes flight-level statistics using LINQ aggregates:
    ///  - GroupBy breakdown of check-in statuses via in-memory LINQ
    ///  - Sum of bag weights across all passengers
    ///  - All() to check whether every passenger is accounted for
    /// </summary>
    public async Task<FlightStatsResponse?> GetStatsAsync(string flightNumber)
    {
        var fn = flightNumber.ToUpperInvariant();

        var flightExists = await _db.Flights.AnyAsync(f => f.FlightNumber == fn);
        if (!flightExists) return null;

        // Fetch passengers with bags in a single query
        var passengers = await _db.Passengers
            .AsNoTracking()
            .Include(p => p.Bags)
            .Where(p => p.FlightNumber == fn)
            .ToListAsync();

        // In-memory LINQ aggregates — clear and readable at this scale
        var statusGroups = passengers
            .GroupBy(p => p.CheckInStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        var allBags = passengers.SelectMany(p => p.Bags).ToList();

        var allAccountedFor = passengers.All(p =>
            p.CheckInStatus == CheckInStatus.Boarded ||
            p.CheckInStatus == CheckInStatus.NoShow);

        return new FlightStatsResponse(
            FlightNumber: fn,
            TotalPassengers: passengers.Count,
            CheckedIn:    statusGroups.GetValueOrDefault(CheckInStatus.CheckedIn),
            Boarded:      statusGroups.GetValueOrDefault(CheckInStatus.Boarded),
            NotCheckedIn: statusGroups.GetValueOrDefault(CheckInStatus.NotCheckedIn),
            NoShows:      statusGroups.GetValueOrDefault(CheckInStatus.NoShow),
            TotalBagWeightKg: allBags.Sum(b => b.WeightKg),
            TotalBags:        allBags.Count,
            AllPassengersAccountedFor: allAccountedFor
        );
    }
}
