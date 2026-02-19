namespace AeroCloud.PPS.Models;

/// <summary>
/// Represents a flight operating through the airport.
/// </summary>
public class Flight
{
    public int Id { get; set; }

    /// <summary>IATA flight number (e.g. "EZY1234").</summary>
    public required string FlightNumber { get; set; }

    /// <summary>IATA origin airport code.</summary>
    public required string Origin { get; set; }

    /// <summary>IATA destination airport code.</summary>
    public required string Destination { get; set; }

    public DateTime ScheduledDeparture { get; set; }

    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

    /// <summary>Allocated departure gate (e.g. "B14").</summary>
    public string? Gate { get; set; }
}

public enum FlightStatus
{
    Scheduled,
    Boarding,
    Departed,
    Cancelled,
    Delayed
}
