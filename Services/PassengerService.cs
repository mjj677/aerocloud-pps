using AeroCloud.PPS.Data;
using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Messaging;
using AeroCloud.PPS.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroCloud.PPS.Services;

public class PassengerService : IPassengerService
{
    private readonly AppDbContext _db;
    private readonly IBoardingEventPublisher _publisher;
    private readonly ILogger<PassengerService> _logger;

    public PassengerService(
        AppDbContext db,
        IBoardingEventPublisher publisher,
        ILogger<PassengerService> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PassengerResponse?> GetByBookingReferenceAsync(string bookingReference)
    {
        var passenger = await _db.Passengers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.BookingReference == bookingReference.ToUpperInvariant());

        return passenger is null ? null : MapToResponse(passenger);
    }

    /// <summary>
    /// Flexible passenger search using optional LINQ filters.
    /// Each filter is only applied when a value is provided — this builds up
    /// the query conditionally rather than loading everything and filtering in memory.
    /// </summary>
    public async Task<IEnumerable<PassengerResponse>> SearchAsync(PassengerSearchQuery query)
    {
        // Start with the full set — IQueryable, nothing sent to DB yet
        var q = _db.Passengers.AsNoTracking().AsQueryable();

        // Conditionally chain Where clauses — only active when the filter has a value
        if (!string.IsNullOrWhiteSpace(query.FlightNumber))
            q = q.Where(p => p.FlightNumber == query.FlightNumber.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(query.FullName))
            q = q.Where(p => p.FullName.Contains(query.FullName));

        if (!string.IsNullOrWhiteSpace(query.CheckInStatus)
            && Enum.TryParse<CheckInStatus>(query.CheckInStatus, ignoreCase: true, out var status))
            q = q.Where(p => p.CheckInStatus == status);

        var results = await q.OrderBy(p => p.FullName).ToListAsync();
        return results.Select(MapToResponse);
    }

    public async Task<PassengerResponse> CheckInAsync(CheckInRequest request)
    {
        var passenger = await _db.Passengers
            .FirstOrDefaultAsync(p => p.BookingReference == request.BookingReference.ToUpperInvariant());

        if (passenger is null)
            throw new KeyNotFoundException($"No passenger found for booking reference '{request.BookingReference}'.");

        if (passenger.CheckInStatus == CheckInStatus.Boarded)
            throw new InvalidOperationException("Passenger has already boarded and cannot be re-checked in.");

        passenger.SeatNumber = request.SeatNumber;
        passenger.CheckInStatus = CheckInStatus.CheckedIn;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Passenger {PassengerId} ({FullName}) checked in to seat {Seat} on {Flight}",
            passenger.Id, passenger.FullName, passenger.SeatNumber, passenger.FlightNumber);

        return MapToResponse(passenger);
    }

    public async Task<PassengerResponse?> UpdateBoardingStatusAsync(string bookingReference)
    {
        var passenger = await _db.Passengers
            .FirstOrDefaultAsync(p => p.BookingReference == bookingReference.ToUpperInvariant());

        if (passenger is null) return null;

        if (passenger.CheckInStatus != CheckInStatus.CheckedIn)
            throw new InvalidOperationException(
                $"Cannot board passenger with status '{passenger.CheckInStatus}'. Passenger must be checked in first.");

        passenger.CheckInStatus = CheckInStatus.Boarded;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Passenger {PassengerId} ({FullName}) boarded flight {Flight}",
            passenger.Id, passenger.FullName, passenger.FlightNumber);

        var boardingEvent = new BoardingEvent(
            passenger.BookingReference,
            passenger.FlightNumber,
            passenger.FullName,
            passenger.SeatNumber,
            DateTime.UtcNow
        );

        await _publisher.PublishBoardingEventAsync(boardingEvent);

        return MapToResponse(passenger);
    }

    private static PassengerResponse MapToResponse(Passenger p) => new(
        p.Id,
        p.FullName,
        p.BookingReference,
        p.FlightNumber,
        p.SeatNumber,
        p.CheckInStatus.ToString(),
        p.CreatedAt
    );
}
