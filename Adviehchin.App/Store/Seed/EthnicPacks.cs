namespace Adviehchin.App.Store.Seed;

/// <summary>تعریف یک پک ادویهٔ قومی؛ منبع مشترک دادهٔ فروشگاه و کاروسل صفحهٔ اصلی.</summary>
public sealed record EthnicPack(
    string Slug,
    string Name,
    string Tagline,
    string ShortDescription,
    string Description,
    string ImageUrl,
    string Sku);

/// <summary>پک‌های قومی؛ محصولات محلی (بدون شناسهٔ منبع) که در دستهٔ «پک‌های هدیه» قرار می‌گیرند.</summary>
public static class EthnicPacks
{
    public const long PriceTomans = 1_250_000;
    public const string CategorySlug = "gifts";
    public const string Unit = "ست هدیه";
    public const int StockQuantity = 50;

    public static IReadOnlyList<EthnicPack> All { get; } =
    [
        new("pack-baluchi", "پک بلوچی", "کاری و ماهی جنوبی",
            "ادویه‌های جنوبی برای کاری، ماهی و برنج بلوچی.",
            "پک بلوچی با فلفل قرمز، زیره، گشنیز، زردچوبه و ترکیب مخصوص کاری و ماهی جنوبی؛ تندی گرم و عطر جنوب در یک ست.",
            "/images/store/ethnic/baluchi.jpg", "ETH-BAL"),
        new("pack-bandari", "پک بندری", "ماهی و میگوی بندری",
            "ادویه‌های معطر برای ماهی، میگو و غذاهای بندری.",
            "پک بندری با ادویهٔ ماهی و میگو، فلفل قرمز، زیره، گشنیز و ترکیب مخصوص غذاهای بندری؛ عطری گرم و دریایی برای سفره‌های جنوب.",
            "/images/store/ethnic/bandari.jpg", "ETH-BND"),
        new("pack-turki", "پک ترکی", "آش و کوفتهٔ ترکی",
            "ترکیب ادویه‌های آش، کوفته و خورش ترکی.",
            "پک ترکی با الهام از آشپزی آذربایجان: زعفران، دارچین، نعناع خشک، فلفل و ترکیب مخصوص آش، کوفته و خورش‌های ترکی. عطری گرم و متعادل برای غذاهای اصیل.",
            "/images/store/ethnic/turki.jpg", "ETH-TRK"),
        new("pack-khozestani", "پک خوزستانی", "قلیه و خورش جنوبی",
            "ادویه‌های گرم برای قلیه، ماهی و خورش‌های خوزستانی.",
            "پک خوزستانی با ادویهٔ قلیه، فلفل قرمز، زیره، گشنیز و ترکیب مخصوص خورش‌ها و غذاهای جنوب؛ تندی متعادل و عطر گرم برای طعم اصیل خوزستان.",
            "/images/store/ethnic/khozestani.jpg", "ETH-KHZ"),
        new("pack-gilaki", "پک گیلکی", "مرغ و ماهی گیلانی",
            "ادویه‌های معطر برای مرغ، ماهی و سبزی‌پلو.",
            "پک گیلکی با گلپر، سیر خشک، فلفل قرمز، نعناع و ترکیب مخصوص ماهی و مرغ؛ طعمی تازه از جنگل و دریا برای غذاهای شمالی.",
            "/images/store/ethnic/gilaki.jpg", "ETH-GIL"),
        new("pack-kordi", "پک کوردی", "کباب و دلمهٔ کردی",
            "ادویه‌های کوهستانی برای کباب و دلمهٔ کردی.",
            "پک کوردی ادویه‌چین مجموعه‌ای از ادویه‌های معطر کوهستان است: سماق، فلفل سیاه، زیره، گل محمدی و ترکیب مخصوص کباب و دلمه. نسبت‌ها طوری انتخاب شده که طعم اصیل غذاهای کردی را در آشپزخانهٔ شما بازسازی کند.",
            "/images/store/ethnic/kordi.jpg", "ETH-KUR"),
        new("pack-khorasani", "پک خراسانی", "زعفران و زرشک",
            "ادویه‌های زعفرانی برای شله، قیمه و غذاهای خراسانی.",
            "پک خراسانی با زعفران، زرشک، زیره، دارچین و ترکیب مخصوص شله، قیمه و غذاهای خراسانی؛ رنگ و عطری که از دشت‌های خراسان می‌آید.",
            "/images/store/ethnic/khorasani.jpg", "ETH-KHR")
    ];
}
