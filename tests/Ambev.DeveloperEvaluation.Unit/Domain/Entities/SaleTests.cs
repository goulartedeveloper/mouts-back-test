using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Unit.Domain.Entities.TestData;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Fact(DisplayName = "Sale total is the sum of non-cancelled items")]
    public void Given_SaleWithItems_When_TotalCalculated_Then_SumsActiveItemsOnly()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 0);
        sale.AddItem(Guid.NewGuid(), "A", 5, 10m);   // 45 (10% discount)
        sale.AddItem(Guid.NewGuid(), "B", 2, 10m);   // 20 (no discount)
        var item3 = sale.AddItem(Guid.NewGuid(), "C", 3, 10m); // 30 (no discount)

        Assert.Equal(95, sale.TotalAmount);

        sale.CancelItem(item3.Id);
        Assert.Equal(65, sale.TotalAmount);
    }

    [Fact(DisplayName = "Cancelling sale flips status to Cancelled and zeroes total")]
    public void Given_ActiveSale_When_Cancelled_Then_StatusIsCancelledAndTotalIsZero()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 2);
        Assert.True(sale.TotalAmount > 0);

        sale.Cancel();

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.True(sale.IsCancelled);
        Assert.Equal(0, sale.TotalAmount);
    }

    [Fact(DisplayName = "Cannot add items to a cancelled sale")]
    public void Given_CancelledSale_When_AddItem_Then_ShouldThrow()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 1);
        sale.Cancel();

        var ex = Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), "X", 1, 10m));
        Assert.Contains("cancelled sale", ex.Message);
    }

    [Fact(DisplayName = "Cancelling an unknown item should throw")]
    public void Given_Sale_When_CancelUnknownItem_Then_ShouldThrow()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 1);

        var ex = Assert.Throws<DomainException>(() => sale.CancelItem(Guid.NewGuid()));
        Assert.Contains("does not belong", ex.Message);
    }

    [Fact(DisplayName = "ReplaceItems swaps the item collection and recalculates the total")]
    public void Given_Sale_When_ReplaceItems_Then_TotalReflectsNewItems()
    {
        var sale = SaleTestData.GenerateValidSale(itemCount: 1);

        var newItem = new SaleItem(Guid.NewGuid(), "New", 10, 10m); // 80 (20% discount)
        sale.ReplaceItems(new[] { newItem });

        Assert.Single(sale.Items);
        Assert.Equal(80, sale.TotalAmount);
    }
}
