namespace AeroCloud.PPS.Models;

/// <summary>
/// Represents a passenger booked on a flight.
/// </summary>
public class Passenger
{
    public int Id { get; set; }

    /// <summary>Passenger's full name as it appears on their travel document.</summary>
    public required string FullName { get; set; }

    /// <summary>Booking reference / PNR (e.g. "ABC123").</summary>
    public required string BookingReference { get; set; }

    /// <summary>IATA flight identifier (e.g. "EZY1234").</summary>
    public required string FlightNumber { get; set; }

    /// <summary>Seat allocated at check-in.</summary>
    public string? SeatNumber { get; set; }

    public CheckInStatus CheckInStatus { get; set; } = CheckInStatus.NotCheckedIn;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Bags registered to this passenger.</summary>
    public ICollection<BagDrop> Bags { get; set; } = new List<BagDrop>();
}

public enum CheckInStatus
{
    NotCheckedIn,
    CheckedIn,
    Boarded,
    NoShow
}
