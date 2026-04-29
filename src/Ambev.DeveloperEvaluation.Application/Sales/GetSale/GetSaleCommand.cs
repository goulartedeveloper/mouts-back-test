using Ambev.DeveloperEvaluation.Application.Sales.Common;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleCommand : IRequest<SaleResult>
{
    public Guid Id { get; set; }

    public GetSaleCommand() { }
    public GetSaleCommand(Guid id) => Id = id;
}
