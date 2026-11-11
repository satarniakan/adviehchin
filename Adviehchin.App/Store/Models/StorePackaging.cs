namespace Adviehchin.App.Store.Models;

/// <summary>یک نوع بسته‌بندی؛ <paramref name="Summary"/> زیر دکمه و <paramref name="Guide"/> در راهنما می‌آید.</summary>
public sealed record StorePackagingOption(string Key, string Title, string Summary, string Guide, string Icon);

/// <summary>
/// سه نوع بسته‌بندی ادویه‌چین. انتخاب مشتری روی قیمت اثری ندارد و همراه سفارش ثبت می‌شود.
/// </summary>
public static class StorePackaging
{
    public const string DefaultKey = "normal";

    public static readonly IReadOnlyList<StorePackagingOption> All =
    [
        new("normal", "معمولی", "نایلون ساده و اقتصادی",
            "بسته‌بندی معمولی همان نایلونِ ساده و سبک است؛ انتخابی اقتصادی برای مصرف روزمره. ادویه در آن تمیز و به‌دور از رطوبت می‌ماند و برای کسی که زودبه‌زود از ادویه استفاده می‌کند، کاملاً کافی است.",
            "bi-bag"),
        new("amirza", "آمیرزا", "بانکهٔ شیشه‌ای درب‌دار",
            "در بسته‌بندی آمیرزا، ادویه در بانکه‌های شیشه‌ای درب‌دار عرضه می‌شود؛ شفاف، خوش‌دست و قابل شست‌وشو. اگر می‌خواهید ادویه‌تان همیشه روی کابینت آشپزخانه در دسترس باشد و عطر و رنگش ماه‌ها سر جای خودش بماند، آمیرزا انتخاب درست است.",
            "bi-box2"),
        new("shahpasand", "شاهپسند", "بسته‌بندی ویژه و چوبی با باکس",
            "شاهپسند بسته‌بندی ویژه و چوبی با باکس است؛ شکیل و محکم، مناسب هدیه دادن و پذیرایی. چوب و باکسِ آن هم از ادویه در برابر نور و ضربه محافظت می‌کند و هم سفرهٔ شما را چشم‌نوازتر.",
            "bi-gift")
    ];

    /// <summary>کلید نامعتبر یا خالی به بسته‌بندی معمولی برمی‌گردد.</summary>
    public static StorePackagingOption Get(string? key) =>
        All.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static string Normalize(string? key) => Get(key).Key;
}