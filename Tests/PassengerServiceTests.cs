using AeroCloud.PPS.Data;
using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Messaging;
using AeroCloud.PPS.Models;
using AeroCloud.PPS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AeroCloud.PPS.Tests;

/// <summary>
/// Unit tests for PassengerService.
/// Uses EF Core's in-memory provider for DB isolation.
/// Uses Moq to verify IBoardingEventPublisher is called correctly on boarding.
/// </summary>
public class PassengerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IBoardingEventPublisher> _publisherMock;
    private readonly PassengerService _sut;

    public PassengerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _publisherMock = new Mock<IBoardingEventPublisher>();
        _sut = new PassengerService(_db, _publisherMock.Object, NullLogger<PassengerService>.Instance);
    }

    // ── GetByBookingReference ────────────────────────────────────────────────

    [Fact]
    public async Task GetByBookingReferenceAsync_ReturnsPassenger_WhenFound()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "John Doe",
            BookingReference = "XYZ999",
            FlightNumber = "EZY1234"
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetByBookingReferenceAsync("XYZ999");

        result.Should().NotBeNull();
        result!.BookingReference.Should().Be("XYZ999");
    }

    [Fact]
    public async Task GetByBookingReferenceAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByBookingReferenceAsync("DOESNOTEXIST");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByBookingReferenceAsync_IsCaseInsensitive()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Jane Smith",
            BookingReference = "ABC123",
            FlightNumber = "BA0456"
        });
        await _db.SaveChangesAsync();

        var result = await _sut.GetByBookingReferenceAsync("abc123");

        result.Should().NotBeNull();
        result!.BookingReference.Should().Be("ABC123");
    }

    // ── CheckIn ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CheckInAsync_SetsStatusToCheckedIn_AndAllocatesSeat()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Alice Brown",
            BookingReference = "CHK001",
            FlightNumber = "EZY1234",
            CheckInStatus = CheckInStatus.NotCheckedIn
        });
        await _db.SaveChangesAsync();

        var result = await _sut.CheckInAsync(new CheckInRequest("CHK001", "22B"));

        result.CheckInStatus.Should().Be("CheckedIn");
        result.SeatNumber.Should().Be("22B");
    }

    [Fact]
    public async Task CheckInAsync_Throws_WhenBookingReferenceNotFound()
    {
        var act = async () => await _sut.CheckInAsync(new CheckInRequest("NOSUCHREF", "1A"));

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*NOSUCHREF*");
    }

    [Fact]
    public async Task CheckInAsync_Throws_WhenPassengerAlreadyBoarded()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Bob Green",
            BookingReference = "BRD001",
            FlightNumber = "EZY1234",
            CheckInStatus = CheckInStatus.Boarded
        });
        await _db.SaveChangesAsync();

        var act = async () => await _sut.CheckInAsync(new CheckInRequest("BRD001", "5C"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already boarded*");
    }

    // ── UpdateBoardingStatus ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBoardingStatusAsync_SetsStatusToBoarded_WhenCheckedIn()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Carol White",
            BookingReference = "BRD002",
            FlightNumber = "EZY1234",
            CheckInStatus = CheckInStatus.CheckedIn
        });
        await _db.SaveChangesAsync();

        var result = await _sut.UpdateBoardingStatusAsync("BRD002");

        result.Should().NotBeNull();
        result!.CheckInStatus.Should().Be("Boarded");
    }

    [Fact]
    public async Task UpdateBoardingStatusAsync_PublishesBoardingEvent_WhenSuccessful()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Dan Black",
            BookingReference = "EVT001",
            FlightNumber = "EZY1234",
            SeatNumber = "10C",
            CheckInStatus = CheckInStatus.CheckedIn
        });
        await _db.SaveChangesAsync();

        await _sut.UpdateBoardingStatusAsync("EVT001");

        // Verify the publisher was called exactly once with the correct booking ref
        _publisherMock.Verify(
            p => p.PublishBoardingEventAsync(
                It.Is<BoardingEvent>(e =>
                    e.BookingReference == "EVT001" &&
                    e.FlightNumber == "EZY1234"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateBoardingStatusAsync_ReturnsNull_WhenPassengerNotFound()
    {
        var result = await _sut.UpdateBoardingStatusAsync("GHOST");
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBoardingStatusAsync_Throws_WhenPassengerNotCheckedIn()
    {
        _db.Passengers.Add(new Passenger
        {
            FullName = "Eve Grey",
            BookingReference = "NOCHK",
            FlightNumber = "BA0456",
            CheckInStatus = CheckInStatus.NotCheckedIn
        });
        await _db.SaveChangesAsync();

        var act = async () => await _sut.UpdateBoardingStatusAsync("NOCHK");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*checked in*");
    }

    public void Dispose() => _db.Dispose();
}
