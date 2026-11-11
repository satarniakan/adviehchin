using Adviehchin.Application.Jobs;
using Adviehchin.Domain.Exceptions;

namespace Adviehchin.App.Store.Sync;

/// <summary>
/// همگام‌سازی خودکار کاتالوگ با سایت منبع، هر ۱۲ ساعت.
/// اولین اجرا بلافاصله پس از استارتاپ انجام می‌شود (رفتار <c>RecurringJobRunner</c>)؛
/// پس اگر اپلیکیشن چند ساعت خاموش بوده، کاتالوگ در اولین بالا آمدن تازه می‌شود.
/// </summary>
public sealed class StoreCatalogSyncJob : IRecurringJob
{
    public const string JobName = "store-catalog-sync";

    public string Name => JobName;

    public TimeSpan Interval => TimeSpan.FromHours(12);

    private readonly IStoreCatalogSyncService _syncService;
    private readonly IServiceProvider _services;
    private readonly ILogger<StoreCatalogSyncJob> _logger;

    public StoreCatalogSyncJob(
        IStoreCatalogSyncService syncService,
        IServiceProvider services,
        ILogger<StoreCatalogSyncJob> logger)
    {
        _syncService = syncService;
        _services = services;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _syncService.SyncAsync(cancellationToken);
            _logger.LogInformation(
                "همگام‌سازی زمان‌بندی‌شدهٔ کاتالوگ در {Seconds:0.0}s انجام شد (اولین اجرا: {First})",
                result.Duration.TotalSeconds, result.IsFirstSync);

            // اولین همگام‌سازی محصولات محلی را غیرفعال می‌کند؛ پک‌های قومی باید برگردند.
            try
            {
                await Adviehchin.App.Store.Seed.StoreSeeder.EnsureEthnicPacksAsync(_services);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "بازسازی پک‌های قومی پس از همگام‌سازی ناموفق بود");
            }
        }
        catch (BusinessRuleException ex)
        {
            // اگر ادمین همان لحظه دکمهٔ «همگام‌سازی الان» را زده باشد، اجرای هم‌زمان مجاز
            // نیست؛ این خطای job نیست و دور بعدی کافی است.
            _logger.LogWarning("همگام‌سازی زمان‌بندی‌شده اجرا نشد: {Message}", ex.Message);
        }
    }
}
