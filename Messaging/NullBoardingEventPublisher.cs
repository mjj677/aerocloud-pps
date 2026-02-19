namespace AeroCloud.PPS.Messaging;

/// <summary>
/// A no-op implementation of IBoardingEventPublisher used when Service Bus
/// is not configured (e.g. local dev without Azure credentials).
///
/// This is the Null Object pattern — it satisfies the interface contract
/// but does nothing. The alternative would be scattering null checks
/// or feature flags throughout the service layer.
///
/// Registered in DI when "Azure:ServiceBus:ConnectionString" is missing/placeholder.
/// </summary>
public class NullBoardingEventPublisher : IBoardingEventPublisher
{
    private readonly ILogger<NullBoardingEventPublisher> _logger;

    public NullBoardingEventPublisher(ILogger<NullBoardingEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishBoardingEventAsync(BoardingEvent boardingEvent, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[NullPublisher] Service Bus not configured — boarding event for {BookingReference} was NOT published.",
            boardingEvent.BookingReference);

        return Task.CompletedTask;
    }
}
