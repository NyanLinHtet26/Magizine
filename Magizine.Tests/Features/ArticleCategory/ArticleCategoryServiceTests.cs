using System.Security.Claims;
using Magizine.DataBase;
using Magizine.DataBase.Models;
using Magizine.Shared.Models.ArticleCategory;
using Magizine.Shared.Security;
using MagizineAdmin.Api.Features.ArticleCategory;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Magizine.Tests.Features.ArticleCategory;

public class ArticleCategoryServiceTests : IDisposable
{
    private readonly MagizineDbContext _dbContext;
    private readonly Mock<ILogger<ArticleCategoryService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly ArticleCategoryService _service;

    public ArticleCategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<MagizineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new MagizineDbContext(options);
        _mockLogger = new Mock<ILogger<ArticleCategoryService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(MagizineClaims.Subject, "123") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _service = new ArticleCategoryService(_dbContext, _mockLogger.Object, _mockHttpContextAccessor.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Create_GeneratesCorrectSlug_WhenSlugIsOmitted()
    {
        var req = new CreateArticleCategoryReqModel
        {
            Name = "Tech News",
            Description = "Latest tech news"
        };

        var result = await _service.CreateArticleCategory(req);

        Assert.True(result.IsSuccess);
        Assert.Equal("tech-news", result.Data!.Slug);
        var inDb = await _dbContext.TblArticleCategories.FirstOrDefaultAsync(c => c.Name == "Tech News");
        Assert.NotNull(inDb);
        Assert.Equal("tech-news", inDb.Slug);
    }

    [Fact]
    public async Task Create_ReturnsError_OnDuplicateSlug()
    {
        // Simulating the Npgsql exception behavior in a Moq DbContext
        // Because InMemory doesn't throw Postgres unique constraints, we'll mock the DbContext for this specific test
        var options = new DbContextOptionsBuilder<MagizineDbContext>().UseInMemoryDatabase("mock_db").Options;
        var mockDb = new Mock<MagizineDbContext>(options);
        
        mockDb.Setup(d => d.TblArticleCategories).Returns(_dbContext.TblArticleCategories);
        mockDb.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new DbUpdateException("Duplicate", new Npgsql.PostgresException("23505", "error", "error", "23505")));
              
        var serviceWithMockDb = new ArticleCategoryService(mockDb.Object, _mockLogger.Object, _mockHttpContextAccessor.Object);
        
        var req = new CreateArticleCategoryReqModel { Name = "Tech News" };
        var result = await serviceWithMockDb.CreateArticleCategory(req);

        Assert.False(result.IsSuccess);
        Assert.Equal("A category with this slug already exists.", result.RespCode);
    }

    [Fact]
    public async Task Delete_PerformsSoftDelete()
    {
        var category = new TblArticleCategory { Name = "ToDelete", Slug = "to-delete" };
        _dbContext.TblArticleCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        var req = new ArticleCategoryDetailReqModel { ArticleCategoryId = category.ArticleCategoryId };
        
        var result = await _service.DeleteArticleCategory(req);

        Assert.True(result.IsSuccess);
        var inDb = await _dbContext.TblArticleCategories.IgnoreQueryFilters().FirstAsync(c => c.ArticleCategoryId == category.ArticleCategoryId);
        Assert.True(inDb.IsDeleted);
        Assert.NotNull(inDb.DeletedAt);
        Assert.Equal(123, inDb.DeletedByAdminId); // From the mock HTTP Context Subject claim
    }

    [Fact]
    public async Task Update_ModifiesExistingRecord()
    {
        var category = new TblArticleCategory { Name = "Old Name", Slug = "old-name" };
        _dbContext.TblArticleCategories.Add(category);
        await _dbContext.SaveChangesAsync();

        var req = new UpdateArticleCategoryReqModel
        {
            ArticleCategoryId = category.ArticleCategoryId,
            Name = "New Name",
            Description = "Updated desc"
        };

        var result = await _service.UpdateArticleCategory(req);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Data!.Name);
        Assert.Equal("new-name", result.Data.Slug);
        
        var inDb = await _dbContext.TblArticleCategories.FirstAsync(c => c.ArticleCategoryId == category.ArticleCategoryId);
        Assert.Equal("New Name", inDb.Name);
        Assert.NotNull(inDb.UpdatedAt);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenIdDoesNotExist()
    {
        var req = new ArticleCategoryDetailReqModel { ArticleCategoryId = 999 };
        
        var result = await _service.GetArticleCategoryById(req);

        Assert.False(result.IsSuccess);
        Assert.Equal("Category not found.", result.RespCode);
    }
}
