using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;

public class SaleApiProfile : Profile
{
    public SaleApiProfile()
    {
        CreateMap<SaleItemRequestModel, SaleItemInput>();

        CreateMap<SaleItemResult, SaleItemResponseModel>();
        CreateMap<SaleResult, SaleResponseModel>();

        CreateMap<CreateSaleRequest, CreateSaleCommand>();
        CreateMap<UpdateSaleRequest, UpdateSaleCommand>();
    }
}
