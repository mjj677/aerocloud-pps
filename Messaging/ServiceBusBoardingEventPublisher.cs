using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace AeroCloud.PPS.Messaging;

/// <summary>
/// Publishes boarding events to an Azure Service Bus topic.
///
/// AZURE SERVICE BUS CONCEPTS:
/// - Namespace: the top-level container (like an SQS/SNS account-level resource)
/// - Topic: a named channel for a category of messages (like SNS topic)
/// - Subscription: a named filter on a topic — downstream consumers each get their own
///   subscription so they all receive every message independently (fan-out)
/// - Message: a JSON payload with optional metadata (correlation ID, content type etc.)
///
/// Compare to AWS:
///   Service Bus Topic  ≈  SNS Topic
///   Subscription       ≈  SQS queue subscribed to SNS
///   Queue (not used here) ≈ SQS queue (point-to-point, one consumer)
///
/// When a passenger boards, we publish one BoardingEvent message.
/// Multiple downstream systems (baggage reconciliation, catering, DCS) each have
/// their own subscription and receive it independently — this is the fan-out pattern.
/// </summary>
public class ServiceBusBoardingEventPublisher : IBoardingEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusBoardingEventPublisher> _logger;

    public ServiceBusBoardingEventPublisher(
        IConfiguration config,
        ILogger<ServiceBusBoardingEventPublisher> logger)
    {
        _logger = logger;

        var connectionString = config["Azure:ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Service Bus connection string is not configured.");

        var topicName = config["Azure:ServiceBus:BoardingEventsTopic"]
            ?? "boarding-events";

        // ServiceBusClient manages the underlying AMQP connection.
        // ServiceBusSender is scoped to a single topic/queue.
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    /// <summary>
    /// Serialises the boarding event to JSON and sends it as a Service Bus message.
    /// Sets ContentType and CorrelationId so downstream subscribers can filter/trace.
    /// </summary>
    public async Task PublishBoardingEventAsync(BoardingEvent boardingEvent, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(boardingEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            // CorrelationId lets downstream systems trace this message back to the booking
            CorrelationId = boardingEvent.BookingReference,
            // Subject is a lightweight filter property — subscriptions can filter on this
            // without deserialising the body (cheaper than SQL filter on body content)
            Subject = "passenger.boarded"
        };

        _logger.LogInformation(
            "Publishing boarding event for {BookingReference} on flight {FlightNumber}",
            boardingEvent.BookingReference,
            boardingEvent.FlightNumber);

        await _sender.SendMessageAsync(message, ct);

        _logger.LogInformation(
            "Boarding event published for {BookingReference}",
            boardingEvent.BookingReference);
    }

    // IAsyncDisposable — ensures the AMQP connection is cleanly closed on shutdown
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
