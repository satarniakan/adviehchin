using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Adviehchin.Application.Services;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;
using Adviehchin.Infrastructure.Repositories;

namespace Adviehchin.Tests;

/// <summary>
/// شمارهٔ ترتیبی: از ۱ شروع، پشت‌سرهم، مستقل برای هر نام، و قالب‌بندی با پیشوند/صفر.
/// (مسابقهٔ واقعی همزمانی روی InMemory قابل شبیه‌سازی نیست؛ حلقهٔ retry روی SQL Server است.)
/// </summary>
public class NumberSeriesTests
{
    private static (NumberSeries series, AdviehchinDbContext db) Build()
    {
        var options = new DbContextOptionsBuilder<AdviehchinDbContext>()
            .ReplaceService<IModelCacheKeyFactory, AdviehchinModelCacheKeyFactory>()
            .UseInMemoryDatabase($"numbers-{Guid.NewGuid()}")
            .Options;

        var db = new AdviehchinDbContext(options, Array.Empty<IAdviehchinModule>());

        var uow = new Mock<IAdviehchinUnitOfWork>();
        uow.Setup(u => u.CompleteAsync())
            .Callback(() => db.SaveChanges())
            .ReturnsAsync(1);

        return (new NumberSeries(new NumberSequenceRepository(db), uow.Object), db);
    }

    [Fact]
    public async Task NextAsync_StartsAtOne_ThenIncrements()
    {
        var (series, _) = Build();

        Assert.Equal(1, await series.NextAsync("invoice"));
        Assert.Equal(2, await series.NextAsync("invoice"));
        Assert.Equal(3, await series.NextAsync("invoice"));
    }

    [Fact]
    public async Task NextAsync_DifferentNames_AreIndependent()
    {
        var (series, _) = Build();

        Assert.Equal(1, await series.NextAsync("invoice"));
        Assert.Equal(1, await series.NextAsync("contract"));
        Assert.Equal(2, await series.NextAsync("invoice"));
    }

    [Fact]
    public async Task NextFormattedAsync_PadsWithZeros()
    {
        var (series, _) = Build();

        Assert.Equal("1405-000001", await series.NextFormattedAsync("invoice-1405", "1405-"));
        Assert.Equal("1405-000002", await series.NextFormattedAsync("invoice-1405", "1405-"));
    }

    [Fact]
    public async Task NextAsync_BlankName_Throws()
    {
        var (series, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => series.NextAsync("  "));
    }
}
