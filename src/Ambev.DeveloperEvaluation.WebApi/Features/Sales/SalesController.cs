using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// REST endpoints for the Sales aggregate.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new sale.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponseModel>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var command = _mapper.Map<CreateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);
        var response = _mapper.Map<SaleResponseModel>(result);

        return Created(string.Empty, new ApiResponseWithData<SaleResponseModel>
        {
            Success = true,
            Message = "Sale created successfully",
            Data = response
        });
    }

    /// <summary>
    /// Retrieves a sale by id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleCommand(id), cancellationToken);
        return Ok(_mapper.Map<SaleResponseModel>(result));
    }

    /// <summary>
    /// Lists sales with pagination, filtering and ordering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SaleResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSales(
        [FromQuery(Name = "_page")] int? page,
        [FromQuery(Name = "_size")] int? size,
        [FromQuery(Name = "_order")] string? order,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? saleNumber,
        [FromQuery] string? customerName,
        [FromQuery] string? branchName,
        [FromQuery(Name = "_minSaleDate")] DateTime? minSaleDate,
        [FromQuery(Name = "_maxSaleDate")] DateTime? maxSaleDate,
        [FromQuery(Name = "_minTotalAmount")] decimal? minTotalAmount,
        [FromQuery(Name = "_maxTotalAmount")] decimal? maxTotalAmount,
        [FromQuery] bool? isCancelled,
        CancellationToken cancellationToken)
    {
        var command = new ListSalesCommand
        {
            Page = page is null or < 1 ? 1 : page.Value,
            Size = size is null or < 1 ? 10 : size.Value,
            Order = order,
            CustomerId = customerId,
            BranchId = branchId,
            SaleNumber = saleNumber,
            CustomerName = customerName,
            BranchName = branchName,
            MinSaleDate = minSaleDate,
            MaxSaleDate = maxSaleDate,
            MinTotalAmount = minTotalAmount,
            MaxTotalAmount = maxTotalAmount,
            IsCancelled = isCancelled
        };

        var result = await _mediator.Send(command, cancellationToken);
        var items = result.Items.Select(_mapper.Map<SaleResponseModel>).ToList();

        var paginated = new PaginatedResponse<SaleResponseModel>
        {
            Success = true,
            Data = items,
            CurrentPage = result.CurrentPage,
            TotalPages = result.TotalPages,
            TotalItems = result.TotalItems
        };

        // Bypass BaseController.Ok<T> (which would double-wrap in another ApiResponseWithData).
        return new OkObjectResult(paginated);
    }

    /// <summary>
    /// Updates an existing sale (replaces editable state and items).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSale(
        [FromRoute] Guid id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken cancellationToken)
    {
        request.Id = id;

        var validator = new UpdateSaleRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var command = _mapper.Map<UpdateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(_mapper.Map<SaleResponseModel>(result));
    }

    /// <summary>
    /// Deletes a sale permanently.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);
        // BaseController.Ok<T>(T) is generic, so passing ApiResponse here would wrap it
        // inside another ApiResponseWithData (double envelope). Return directly instead.
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Sale deleted successfully" });
    }

    /// <summary>
    /// Cancels a sale (soft action: keeps history but flips status).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(id), cancellationToken);
        return Ok(_mapper.Map<SaleResponseModel>(result));
    }

    /// <summary>
    /// Cancels a single item in a sale.
    /// </summary>
    [HttpPost("{id:guid}/items/{itemId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSaleItem(
        [FromRoute] Guid id,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(id, itemId), cancellationToken);
        return Ok(_mapper.Map<SaleResponseModel>(result));
    }
}
