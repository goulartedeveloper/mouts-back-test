using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesCommand, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListSalesValidator();
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var query = new SaleListQuery(
            request.Page,
            request.Size,
            request.Order,
            request.CustomerId,
            request.BranchId,
            request.SaleNumber,
            request.CustomerName,
            request.BranchName,
            request.MinSaleDate,
            request.MaxSaleDate,
            request.MinTotalAmount,
            request.MaxTotalAmount,
            request.IsCancelled);

        var (sales, totalCount) = await _saleRepository.ListAsync(query, cancellationToken);

        return new ListSalesResult
        {
            Items = sales.Select(s => _mapper.Map<SaleResult>(s)).ToList(),
            TotalItems = totalCount,
            CurrentPage = request.Page,
            PageSize = request.Size
        };
    }
}
