namespace AeroCloud.PPS.Models;

/// <summary>
/// Represents a bag registered through the bag drop process.
/// </summary>
public class BagDrop
{
    public int Id { get; set; }

    /// <summary>10-digit IATA bag tag number.</summary>
    public required string BagTagNumber { get; set; }

    public decimal WeightKg { get; set; }

    public BagStatus Status { get; set; } = BagStatus.Registered;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int PassengerId { get; set; }
    public Passenger? Passenger { get; set; }
}

public enum BagStatus
{
    Registered,
    Screened,
    Loaded,
    Offloaded
}
