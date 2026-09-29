using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Magizine.DataBase;
using Magizine.Shared.Models;
using Magizine.Shared.Models.ArticleCategory;
using Magizine.Shared.Models.Paging;
using Magizine.Shared.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Magizine.Tests.Features.ArticleCategory;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(MagizineClaims.Subject, "123") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class ArticleCategoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNamingPolicy = null
    };

    public ArticleCategoryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext configurations
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<MagizineDbContext>) || d.ServiceType == typeof(DbContextOptions)).ToList();
                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }
                
                var connectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(System.Data.Common.DbConnection));
                if (connectionDescriptor != null) services.Remove(connectionDescriptor);

                // Add InMemory database with a dedicated internal service provider to avoid Npgsql conflict
                var provider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<MagizineDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationDb");
                    options.UseInternalServiceProvider(provider);
                });

                // Override authentication
                services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        // Ensure default auth header
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("TestScheme");
    }

    [Fact]
    public async Task Create_ShouldReturnSuccess()
    {
        var req = new CreateArticleCategoryReqModel
        {
            Name = "Integration Test",
            Description = "Integration Test Desc",
            SortOrder = 1
        };

        var response = await _client.PostAsJsonAsync("/api/admin/article-category/Create", req);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Result<ArticleCategoryResModel>>(_jsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Failed with: {result.RespCode} - {result.RespDesp}");
        Assert.Equal("Integration Test", result.Data!.Name);
        Assert.Equal("integration-test", result.Data.Slug);
    }

    [Fact]
    public async Task List_ShouldReturnPagedResults()
    {
        // Setup initial data
        var req = new CreateArticleCategoryReqModel { Name = "List Test" };
        await _client.PostAsJsonAsync("/api/admin/article-category/Create", req);

        var pageReq = new PageRequest { Page = 1, PageSize = 10 };
        var response = await _client.PostAsJsonAsync("/api/admin/article-category/List", pageReq);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Result<PagedResult<ArticleCategoryResModel>>>(_jsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Failed with: {result.RespCode} - {result.RespDesp}");
        Assert.NotEmpty(result.Data!.Items);
    }

    [Fact]
    public async Task Detail_ShouldReturnCorrectData()
    {
        var req = new CreateArticleCategoryReqModel { Name = "Detail Test" };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/article-category/Create", req);
        var createResult = await createResponse.Content.ReadFromJsonAsync<Result<ArticleCategoryResModel>>(_jsonOptions);
        
        var detailReq = new ArticleCategoryDetailReqModel { ArticleCategoryId = createResult!.Data!.ArticleCategoryId };
        var response = await _client.PostAsJsonAsync("/api/admin/article-category/Detail", detailReq);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Result<ArticleCategoryResModel>>(_jsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Failed with: {result.RespCode} - {result.RespDesp}");
        Assert.Equal(createResult.Data.ArticleCategoryId, result.Data!.ArticleCategoryId);
    }
}
