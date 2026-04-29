using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.WebApi.Events;

/// <summary>
/// Publishes domain events to the application log. The README allows logging
/// in lieu of an actual message broker for the prototype.
/// </summary>
public class LoggingDomainEventPublisher : IDomainEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<LoggingDomainEventPublisher> _logger;

    public LoggingDomainEventPublisher(ILogger<LoggingDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions);
        _logger.LogInformation(
            "DomainEvent {EventType} occurred at {OccurredAt}: {Payload}",
            typeof(TEvent).Name,
            @event.OccurredAt,
            payload);
        return Task.CompletedTask;
    }
}
