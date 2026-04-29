using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Validation;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Represents a single line item within a <see cref="Sale"/>.
/// Holds the External Identity of the product plus a denormalized description,
/// the purchased quantity, the unit price and the applied discount rules.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>
    /// Maximum quantity of identical items allowed per item line.
    /// </summary>
    public const int MaxQuantity = 20;

    /// <summary>
    /// Minimum quantity that triggers any discount tier.
    /// </summary>
    public const int MinQuantityForDiscount = 4;

    /// <summary>
    /// Quantity threshold at which the second discount tier kicks in.
    /// </summary>
    public const int SecondTierThreshold = 10;

    private const decimal FirstTierDiscount = 0.10m;
    private const decimal SecondTierDiscount = 0.20m;

    /// <summary>
    /// Foreign key to the Sale aggregate root.
    /// </summary>
    public Guid SaleId { get; set; }

    /// <summary>
    /// External Identity of the product (kept as a Guid to allow cross-domain references).
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Denormalized product name (External Identities pattern).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of items being purchased.
    /// Must be greater than zero and not exceed <see cref="MaxQuantity"/>.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Unit price of the product at the time of sale.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Discount applied as a fraction (e.g. 0.10 for 10%).
    /// </summary>
    public decimal Discount { get; private set; }

    /// <summary>
    /// Total amount of the line item after discount: Quantity * UnitPrice * (1 - Discount).
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Indicates whether this line item has been individually cancelled.
    /// A cancelled item does not contribute to the sale total.
    /// </summary>
    public bool IsCancelled { get; private set; }

    public SaleItem()
    {
        Id = Guid.NewGuid();
    }

    public SaleItem(Guid productId, string productName, int quantity, decimal unitPrice)
        : this()
    {
        ProductId = productId;
        ProductName = productName;
        SetQuantityAndPrice(quantity, unitPrice);
    }

    /// <summary>
    /// Updates the quantity / unit price and re-applies the discount rules.
    /// </summary>
    public void SetQuantityAndPrice(int quantity, decimal unitPrice)
    {
        EnsureNotCancelled();
        EnsureQuantityIsValid(quantity);

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");

        Quantity = quantity;
        UnitPrice = unitPrice;
        ApplyDiscountTier();
        RecalculateTotal();
    }

    /// <summary>
    /// Cancels this individual line item.
    /// </summary>
    public void Cancel()
    {
        if (IsCancelled)
            return;

        IsCancelled = true;
        TotalAmount = 0;
    }

    /// <summary>
    /// Performs validation of the SaleItem using <see cref="SaleItemValidator"/>.
    /// </summary>
    public ValidationResultDetail Validate()
    {
        var validator = new SaleItemValidator();
        var result = validator.Validate(this);
        return new ValidationResultDetail
        {
            IsValid = result.IsValid,
            Errors = result.Errors.Select(o => (ValidationErrorDetail)o)
        };
    }

    private void ApplyDiscountTier()
    {
        if (Quantity >= SecondTierThreshold)
            Discount = SecondTierDiscount;
        else if (Quantity >= MinQuantityForDiscount)
            Discount = FirstTierDiscount;
        else
            Discount = 0m;
    }

    private void RecalculateTotal()
    {
        TotalAmount = IsCancelled
            ? 0m
            : Math.Round(Quantity * UnitPrice * (1m - Discount), 2);
    }

    private static void EnsureQuantityIsValid(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        if (quantity > MaxQuantity)
            throw new DomainException($"It is not possible to sell more than {MaxQuantity} identical items.");
    }

    private void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException("Cannot modify a cancelled item.");
    }
}
