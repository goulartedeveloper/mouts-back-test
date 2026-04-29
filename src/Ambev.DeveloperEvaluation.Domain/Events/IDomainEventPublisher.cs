namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>
/// Abstraction for publishing domain events to whatever transport is available
/// (log, in-memory bus, message broker, ...).
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
