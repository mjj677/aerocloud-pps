using AeroCloud.PPS.Data;
using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroCloud.PPS.Services;

public class BagDropService : IBagDropService
{
    private const decimal MaxWeightKg = 32m; // IATA single-bag weight limit
    private const int MaxBagsPerPassenger = 5;

    private readonly AppDbContext _db;
    private readonly ILogger<BagDropService> _logger;

    public BagDropService(AppDbContext db, ILogger<BagDropService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<BagDropResponse>> GetBagsForPassengerAsync(int passengerId)
    {
        return await _db.BagDrops
            .AsNoTracking()
            .Where(b => b.PassengerId == passengerId)
            .Select(b => MapToResponse(b))
            .ToListAsync();
    }

    /// <summary>
    /// Registers a bag against a checked-in passenger. Validates weight limits
    /// and ensures the passenger exists and is checked in before accepting the bag.
    /// </summary>
    public async Task<BagDropResponse> RegisterBagAsync(BagDropRequest request)
    {
        // Validate passenger exists and is checked in
        var passenger = await _db.Passengers.FindAsync(request.PassengerId)
            ?? throw new KeyNotFoundException($"Passenger with ID {request.PassengerId} not found.");

        if (passenger.CheckInStatus != CheckInStatus.CheckedIn)
            throw new InvalidOperationException("Bags can only be registered for checked-in passengers.");

        // Validate weight
        if (request.WeightKg <= 0 || request.WeightKg > MaxWeightKg)
            throw new ArgumentException($"Bag weight must be between 0 and {MaxWeightKg}kg.");

        // Enforce bag count limit
        var existingBagCount = await _db.BagDrops.CountAsync(b => b.PassengerId == request.PassengerId);
        if (existingBagCount >= MaxBagsPerPassenger)
            throw new InvalidOperationException($"Passenger already has the maximum of {MaxBagsPerPassenger} bags registered.");

        var bag = new BagDrop
        {
            PassengerId = request.PassengerId,
            BagTagNumber = request.BagTagNumber,
            WeightKg = request.WeightKg,
            Status = BagStatus.Registered,
            RegisteredAt = DateTime.UtcNow
        };

        _db.BagDrops.Add(bag);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Bag {BagTag} ({Weight}kg) registered for passenger {PassengerId}",
            bag.BagTagNumber, bag.WeightKg, bag.PassengerId);

        return MapToResponse(bag);
    }

    private static BagDropResponse MapToResponse(BagDrop b) => new(
        b.Id,
        b.BagTagNumber,
        b.WeightKg,
        b.Status.ToString(),
        b.RegisteredAt
    );
}
