using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Functional;

/// <summary>
/// Boots the WebApi against an EF Core in-memory database. Each instance gets
/// a fresh isolated database, keyed by a unique name, and exposes a helper to
/// build an HttpClient pre-authenticated with a JWT obtained through the real
/// auth flow (POST /api/users + POST /api/auth).
/// </summary>
public class SalesApiFactory : WebApplicationFactory<Program>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _databaseName = $"functional-{Guid.NewGuid()}";

    /// <summary>Email used when bootstrapping the test user.</summary>
    public const string TestUserEmail = "tester@example.com";

    /// <summary>Password used when bootstrapping the test user.</summary>
    public const string TestUserPassword = "Test@1234";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DefaultContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<DefaultContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Creates an HttpClient with a Bearer token, bootstrapping a test user on
    /// the first call. Idempotent: subsequent calls reuse the existing user.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var token = await EnsureTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> EnsureTokenAsync(HttpClient client)
    {
        // Best-effort registration: if the user already exists the API returns 409 — fine.
        // Status/Role are sent as the underlying int values (UserStatus.Active = 1, UserRole.Admin = 3).
        await client.PostAsJsonAsync("/api/users", new
        {
            username = "tester",
            password = TestUserPassword,
            email = TestUserEmail,
            phone = "+5511999999999",
            status = 1,
            role = 3
        });

        var auth = await client.PostAsJsonAsync("/api/auth", new
        {
            email = TestUserEmail,
            password = TestUserPassword
        });
        auth.EnsureSuccessStatusCode();

        var body = await auth.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // The template's AuthController returns its payload through BaseController.Ok<T>,
        // which double-wraps it ({ data: { data: {...}, success, ... }, success, ... }).
        // Walk both levels to find the token regardless of the wrapper depth.
        var data = body.GetProperty("data");
        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out var inner))
            data = inner;

        return data.GetProperty("token").GetString()!;
    }
}
