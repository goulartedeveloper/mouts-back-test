using Ambev.DeveloperEvaluation.Domain.Entities;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Covers the discount tier rules and quantity restrictions on <see cref="SaleItem"/>.
/// </summary>
public class SaleItemTests
{
    [Theory(DisplayName = "Discount must follow quantity-based tiers")]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void Given_Quantity_When_ItemCreated_Then_DiscountMatchesTier(int quantity, decimal expectedDiscount)
    {
        var item = new SaleItem(Guid.NewGuid(), "Beer", quantity, 10m);

        Assert.Equal(expectedDiscount, item.Discount);
    }

    [Theory(DisplayName = "Total amount equals quantity * unit price * (1 - discount)")]
    [InlineData(2, 10, 0, 20)]
    [InlineData(5, 10, 0.10, 45)]
    [InlineData(10, 10, 0.20, 80)]
    [InlineData(20, 12.50, 0.20, 200)]
    public void Given_QuantityAndPrice_When_ItemCreated_Then_TotalAmountIsCorrect(
        int quantity, decimal price, decimal expectedDiscount, decimal expectedTotal)
    {
        var item = new SaleItem(Guid.NewGuid(), "Beer", quantity, price);

        Assert.Equal(expectedDiscount, item.Discount);
        Assert.Equal(expectedTotal, item.TotalAmount);
    }

    [Fact(DisplayName = "Quantity above maximum should throw DomainException")]
    public void Given_QuantityAbove20_When_ItemCreated_Then_ShouldThrow()
    {
        var ex = Assert.Throws<DomainException>(() => new SaleItem(Guid.NewGuid(), "Beer", 21, 10m));
        Assert.Contains("more than 20 identical items", ex.Message);
    }

    [Fact(DisplayName = "Quantity zero should throw DomainException")]
    public void Given_ZeroQuantity_When_ItemCreated_Then_ShouldThrow()
    {
        var ex = Assert.Throws<DomainException>(() => new SaleItem(Guid.NewGuid(), "Beer", 0, 10m));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact(DisplayName = "Cancelling an item zeros its total")]
    public void Given_ActiveItem_When_Cancelled_Then_TotalAmountBecomesZero()
    {
        var item = new SaleItem(Guid.NewGuid(), "Beer", 5, 10m);
        Assert.Equal(45, item.TotalAmount);

        item.Cancel();

        Assert.True(item.IsCancelled);
        Assert.Equal(0, item.TotalAmount);
    }

    [Fact(DisplayName = "Cannot mutate a cancelled item")]
    public void Given_CancelledItem_When_SetQuantity_Then_ShouldThrow()
    {
        var item = new SaleItem(Guid.NewGuid(), "Beer", 5, 10m);
        item.Cancel();

        Assert.Throws<DomainException>(() => item.SetQuantityAndPrice(2, 10m));
    }
}
