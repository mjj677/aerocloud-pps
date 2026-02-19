namespace AeroCloud.PPS.Messaging;

/// <summary>
/// Publishes passenger boarding events to Azure Service Bus.
/// Abstracted behind an interface so tests can inject a fake/mock
/// without needing a real Service Bus connection.
/// </summary>
public interface IBoardingEventPublisher
{
    /// <summary>
    /// Publishes a BoardingEvent message to the Service Bus topic
    /// after a passenger is successfully marked as boarded.
    /// </summary>
    Task PublishBoardingEventAsync(BoardingEvent boardingEvent, CancellationToken ct = default);
}

/// <summary>
/// The message payload sent to the Service Bus topic.
/// Downstream systems (baggage, catering, DCS) subscribe and react to this.
/// </summary>
public record BoardingEvent(
    string BookingReference,
    string FlightNumber,
    string PassengerName,
    string? SeatNumber,
    DateTime BoardedAtUtc
);
