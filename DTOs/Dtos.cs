using System.ComponentModel.DataAnnotations;

namespace AeroCloud.PPS.DTOs;

// ── Passenger DTOs ──────────────────────────────────────────────────────────

public record PassengerResponse(
    int Id,
    string FullName,
    string BookingReference,
    string FlightNumber,
    string? SeatNumber,
    string CheckInStatus,
    DateTime CreatedAt
);

public record CheckInRequest(
    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Booking reference must be exactly 6 characters.")]
    [RegularExpression(@"^[A-Za-z0-9]{6}$", ErrorMessage = "Booking reference must be alphanumeric.")]
    string BookingReference,

    [Required]
    [StringLength(4, MinimumLength = 2, ErrorMessage = "Seat number must be between 2 and 4 characters.")]
    [RegularExpression(@"^\d{1,3}[A-Z]$", ErrorMessage = "Seat number must be row + letter, e.g. 14A.")]
    string SeatNumber
);

/// <summary>
/// Optional filters for the passenger search endpoint.
/// All fields are optional — any combination can be supplied.
/// </summary>
public record PassengerSearchQuery(
    string? FlightNumber = null,
    string? FullName = null,
    string? CheckInStatus = null
);

// ── BagDrop DTOs ────────────────────────────────────────────────────────────

public record BagDropRequest(
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A valid passenger ID is required.")]
    int PassengerId,

    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Bag tag number must be exactly 10 digits.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Bag tag number must contain only digits.")]
    string BagTagNumber,

    [Required]
    [Range(0.1, 32.0, ErrorMessage = "Bag weight must be between 0.1 and 32kg.")]
    decimal WeightKg
);

public record BagDropResponse(
    int Id,
    string BagTagNumber,
    decimal WeightKg,
    string Status,
    DateTime RegisteredAt
);

// ── Flight DTOs ─────────────────────────────────────────────────────────────

public record FlightResponse(
    int Id,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime ScheduledDeparture,
    string Status,
    string? Gate,
    int PassengerCount
);

/// <summary>
/// One row in the flight manifest — a passenger with their bags inline.
/// Returned by GET /api/flights/{flightNumber}/manifest
/// </summary>
public record ManifestEntry(
    string FullName,
    string BookingReference,
    string? SeatNumber,
    string CheckInStatus,
    IEnumerable<BagDropResponse> Bags
);

/// <summary>
/// Aggregate check-in and baggage statistics for a flight.
/// Returned by GET /api/flights/{flightNumber}/stats
/// </summary>
public record FlightStatsResponse(
    string FlightNumber,
    int TotalPassengers,
    int CheckedIn,
    int Boarded,
    int NotCheckedIn,
    int NoShows,
    decimal TotalBagWeightKg,
    int TotalBags,
    bool AllPassengersAccountedFor
);

// ── Shared ──────────────────────────────────────────────────────────────────

/// <summary>Standard error envelope returned on 4xx/5xx responses.</summary>
public record ErrorResponse(string Error, string? Detail = null);
