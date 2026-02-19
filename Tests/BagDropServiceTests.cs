using AeroCloud.PPS.Data;
using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Models;
using AeroCloud.PPS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AeroCloud.PPS.Tests;

/// <summary>
/// Unit tests for <see cref="BagDropService"/>.
/// </summary>
public class BagDropServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly BagDropService _sut;

    public BagDropServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _sut = new BagDropService(_db, NullLogger<BagDropService>.Instance);
    }

    private async Task<Passenger> SeedCheckedInPassengerAsync()
    {
        var passenger = new Passenger
        {
            FullName = "Test Passenger",
            BookingReference = "TST001",
            FlightNumber = "EZY1234",
            CheckInStatus = CheckInStatus.CheckedIn
        };
        _db.Passengers.Add(passenger);
        await _db.SaveChangesAsync();
        return passenger;
    }

    // ── RegisterBag ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterBagAsync_CreatesBag_ForCheckedInPassenger()
    {
        var passenger = await SeedCheckedInPassengerAsync();

        var request = new BagDropRequest(passenger.Id, "0123456789", 18.5m);
        var result = await _sut.RegisterBagAsync(request);

        result.Should().NotBeNull();
        result.BagTagNumber.Should().Be("0123456789");
        result.WeightKg.Should().Be(18.5m);
        result.Status.Should().Be("Registered");
    }

    [Fact]
    public async Task RegisterBagAsync_Throws_WhenPassengerNotFound()
    {
        var act = async () => await _sut.RegisterBagAsync(new BagDropRequest(9999, "TAG001", 10m));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RegisterBagAsync_Throws_WhenPassengerNotCheckedIn()
    {
        var passenger = new Passenger
        {
            FullName = "Not Checked In",
            BookingReference = "NCI001",
            FlightNumber = "EZY1234",
            CheckInStatus = CheckInStatus.NotCheckedIn
        };
        _db.Passengers.Add(passenger);
        await _db.SaveChangesAsync();

        var act = async () => await _sut.RegisterBagAsync(new BagDropRequest(passenger.Id, "TAG002", 12m));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*checked-in*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(33)]
    public async Task RegisterBagAsync_Throws_WhenWeightIsInvalid(decimal weight)
    {
        var passenger = await SeedCheckedInPassengerAsync();

        var act = async () => await _sut.RegisterBagAsync(new BagDropRequest(passenger.Id, "HEAVYBAG", weight));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*weight*");
    }

    [Fact]
    public async Task RegisterBagAsync_Throws_WhenMaxBagsExceeded()
    {
        var passenger = await SeedCheckedInPassengerAsync();

        // Register 5 bags (the maximum)
        for (int i = 1; i <= 5; i++)
        {
            _db.BagDrops.Add(new BagDrop
            {
                PassengerId = passenger.Id,
                BagTagNumber = $"BAG{i:D10}",
                WeightKg = 10m
            });
        }
        await _db.SaveChangesAsync();

        var act = async () => await _sut.RegisterBagAsync(new BagDropRequest(passenger.Id, "ONEMORE00", 10m));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum*");
    }

    // ── GetBagsForPassenger ──────────────────────────────────────────────────

    [Fact]
    public async Task GetBagsForPassengerAsync_ReturnsOnlyPassengersBags()
    {
        var passenger = await SeedCheckedInPassengerAsync();
        _db.BagDrops.Add(new BagDrop { PassengerId = passenger.Id, BagTagNumber = "MYBAG00001", WeightKg = 20m });
        await _db.SaveChangesAsync();

        var bags = await _sut.GetBagsForPassengerAsync(passenger.Id);

        bags.Should().HaveCount(1);
        bags.First().BagTagNumber.Should().Be("MYBAG00001");
    }

    [Fact]
    public async Task GetBagsForPassengerAsync_ReturnsEmptyList_WhenNoBags()
    {
        var bags = await _sut.GetBagsForPassengerAsync(9999);
        bags.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
