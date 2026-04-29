using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class SaleCreatedEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public DateTime OccurredAt { get; }

    public SaleCreatedEvent(Sale sale)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        CustomerId = sale.CustomerId;
        TotalAmount = sale.TotalAmount;
        OccurredAt = DateTime.UtcNow;
    }
}
