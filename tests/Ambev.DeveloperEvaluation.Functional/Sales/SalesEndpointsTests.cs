using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

public class SalesEndpointsTests : IClassFixture<SalesApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SalesApiFactory _factory;
    private HttpClient _client = default!;

    public SalesEndpointsTests(SalesApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Sales endpoints return 401 without bearer token")]
    public async Task Sales_WithoutToken_Returns401()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/sales/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "POST /api/sales creates a sale and returns 201")]
    public async Task PostSale_ReturnsCreated()
    {
        var payload = BuildCreatePayload("S-FUNC-001");

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("S-FUNC-001", body.GetProperty("data").GetProperty("saleNumber").GetString());
        Assert.True(body.GetProperty("data").GetProperty("totalAmount").GetDecimal() > 0);
    }

    [Fact(DisplayName = "POST /api/sales with quantity above 20 returns 400 with documented error envelope")]
    public async Task PostSale_ItemAboveLimit_ReturnsBadRequest()
    {
        var payload = BuildCreatePayload("S-FUNC-OVER", quantity: 21);

        var response = await _client.PostAsJsonAsync("/api/sales", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("ValidationError", body.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("detail").GetString()));
    }

    [Fact(DisplayName = "GET /api/sales/{id} on missing sale returns 404 with documented error envelope")]
    public async Task GetSale_Missing_Returns404()
    {
        var response = await _client.GetAsync($"/api/sales/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("ResourceNotFound", body.GetProperty("type").GetString());
    }

    [Fact(DisplayName = "GET /api/sales/{id} returns the persisted sale")]
    public async Task GetSale_AfterCreate_Returns200()
    {
        var payload = BuildCreatePayload("S-FUNC-GET");
        var created = await _client.PostAsJsonAsync("/api/sales", payload);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createdBody.GetProperty("data").GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/api/sales/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/sales returns paginated payload with totalItems")]
    public async Task ListSales_ReturnsPaginatedPayloadWithTotalItems()
    {
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/api/sales", BuildCreatePayload($"S-LIST-{i:D3}"));

        var response = await _client.GetAsync("/api/sales?_page=1&_size=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.TryGetProperty("totalItems", out _));
        Assert.True(body.TryGetProperty("currentPage", out _));
        Assert.True(body.TryGetProperty("totalPages", out _));
        Assert.True(body.GetProperty("data").EnumerateArray().Count() <= 2);
    }

    [Fact(DisplayName = "POST /api/sales/{id}/cancel sets status to Cancelled")]
    public async Task CancelSale_FlipsStatus()
    {
        var payload = BuildCreatePayload("S-FUNC-CANCEL");
        var created = await _client.PostAsJsonAsync("/api/sales", payload);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createdBody.GetProperty("data").GetProperty("id").GetGuid();

        var response = await _client.PostAsync($"/api/sales/{id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.GetProperty("data").GetProperty("isCancelled").GetBoolean());
    }

    [Fact(DisplayName = "POST /api/sales/{id}/items/{itemId}/cancel zeroes the cancelled item total")]
    public async Task CancelItem_ZeroesItemTotal()
    {
        var payload = BuildCreatePayload("S-FUNC-ITEM");
        var created = await _client.PostAsJsonAsync("/api/sales", payload);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var saleId = createdBody.GetProperty("data").GetProperty("id").GetGuid();
        var itemId = createdBody.GetProperty("data").GetProperty("items")[0].GetProperty("id").GetGuid();

        var response = await _client.PostAsync($"/api/sales/{saleId}/items/{itemId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var cancelledItem = body.GetProperty("data").GetProperty("items")
            .EnumerateArray()
            .First(i => i.GetProperty("id").GetGuid() == itemId);
        Assert.True(cancelledItem.GetProperty("isCancelled").GetBoolean());
        Assert.Equal(0m, cancelledItem.GetProperty("totalAmount").GetDecimal());
    }

    private static object BuildCreatePayload(string saleNumber, int quantity = 5) => new
    {
        saleNumber,
        saleDate = DateTime.UtcNow,
        customerId = Guid.NewGuid(),
        customerName = "Customer X",
        branchId = Guid.NewGuid(),
        branchName = "Branch Y",
        items = new[]
        {
            new
            {
                productId = Guid.NewGuid(),
                productName = "Beer",
                quantity,
                unitPrice = 10m
            }
        }
    };
}
