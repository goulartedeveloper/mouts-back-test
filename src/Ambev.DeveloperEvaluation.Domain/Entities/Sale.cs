using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a sale aggregate following the External Identities pattern:
/// Customer and Branch are referenced by Id and have their description denormalized
/// in the local entity. Sale Items are owned by the aggregate.
/// </summary>
public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = new();

    /// <summary>
    /// Human-readable sale number (business identifier, unique).
    /// </summary>
    public string SaleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Date when the sale was made.
    /// </summary>
    public DateTime SaleDate { get; set; }

    /// <summary>
    /// External Identity of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Denormalized customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// External Identity of the branch.
    /// </summary>
    public Guid BranchId { get; set; }

    /// <summary>
    /// Denormalized branch name.
    /// </summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>
    /// Total sale amount (sum of non-cancelled item totals).
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Current status of the sale (Active or Cancelled).
    /// </summary>
    public SaleStatus Status { get; private set; }

    /// <summary>
    /// Indicates whether the entire sale has been cancelled.
    /// </summary>
    public bool IsCancelled => Status == SaleStatus.Cancelled;

    /// <summary>
    /// Read-only access to the items belonging to this sale.
    /// </summary>
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Date and time when the sale was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time of the last update.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public Sale()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = SaleStatus.Active;
    }

    /// <summary>
    /// Adds a new item to the sale and recalculates the sale total.
    /// </summary>
    public SaleItem AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        EnsureSaleIsActive();

        var item = new SaleItem(productId, productName, quantity, unitPrice)
        {
            SaleId = Id
        };

        _items.Add(item);
        RecalculateTotal();
        Touch();
        return item;
    }

    /// <summary>
    /// Cancels a specific item by its identifier and recalculates the sale total.
    /// </summary>
    public SaleItem CancelItem(Guid itemId)
    {
        EnsureSaleIsActive();

        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item {itemId} does not belong to this sale.");

        if (item.IsCancelled)
            return item;

        item.Cancel();
        RecalculateTotal();
        Touch();
        return item;
    }

    /// <summary>
    /// Cancels the entire sale: marks the sale as cancelled and zeroes its total.
    /// Item-level state is preserved for auditing purposes.
    /// </summary>
    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            return;

        Status = SaleStatus.Cancelled;
        TotalAmount = 0m;
        Touch();
    }

    /// <summary>
    /// Replaces the current item collection with a new set.
    /// Used by the Update use case to keep the aggregate consistent.
    /// </summary>
    public void ReplaceItems(IEnumerable<SaleItem> newItems)
    {
        EnsureSaleIsActive();

        _items.Clear();
        foreach (var item in newItems)
        {
            item.SaleId = Id;
            _items.Add(item);
        }
        RecalculateTotal();
        Touch();
    }

    /// <summary>
    /// Performs validation of the Sale entity using <see cref="SaleValidator"/>.
    /// </summary>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private void EnsureSaleIsActive()
    {
        if (Status == SaleStatus.Cancelled)
            throw new DomainException("Cannot modify a cancelled sale.");
    }
}
