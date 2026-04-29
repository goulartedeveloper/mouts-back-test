using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class SaleModifiedEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public decimal TotalAmount { get; }
    public DateTime OccurredAt { get; }

    public SaleModifiedEvent(Sale sale)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        TotalAmount = sale.TotalAmount;
        OccurredAt = DateTime.UtcNow;
    }
}
