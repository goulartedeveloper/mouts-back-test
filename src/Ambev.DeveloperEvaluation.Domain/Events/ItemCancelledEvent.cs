using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public class ItemCancelledEvent : IDomainEvent
{
    public Guid SaleId { get; }
    public string SaleNumber { get; }
    public Guid ItemId { get; }
    public Guid ProductId { get; }
    public DateTime OccurredAt { get; }

    public ItemCancelledEvent(Sale sale, SaleItem item)
    {
        SaleId = sale.Id;
        SaleNumber = sale.SaleNumber;
        ItemId = item.Id;
        ProductId = item.ProductId;
        OccurredAt = DateTime.UtcNow;
    }
}
