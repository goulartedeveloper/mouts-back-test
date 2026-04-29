namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>
/// Inbound DTO describing a single item inside a Create/Update Sale command.
/// </summary>
public class SaleItemInput
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
