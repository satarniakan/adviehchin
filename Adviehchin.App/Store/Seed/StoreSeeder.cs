using Adviehchin.App.Store.Entities;
using Adviehchin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Adviehchin.App.Store.Seed;

/// <summary>دادهٔ نمونهٔ فروشگاه؛ فقط وقتی هیچ دسته‌ای وجود ندارد اجرا می‌شود.</summary>
public static class StoreSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdviehchinDbContext>();

        if (await db.Set<StoreCategory>().AnyAsync())
            return;

        var asli = new StoreCategory
        {
            Name = "ادویه‌های اصیل",
            Slug = "asli",
            Description = "زعفران، زردچوبه، فلفل، دارچین و دیگر ادویه‌های پرکاربرد آشپزخانهٔ ایرانی.",
            ImageUrl = "/images/store/products/saffron.jpg",
            SortOrder = 1
        };
        var blends = new StoreCategory
        {
            Name = "ترکیب‌ها و چاشنی",
            Slug = "blends",
            Description = "ادویه‌های آمادهٔ پلو، مرغ، کاری و ترکیب‌های خانگی.",
            ImageUrl = "/images/store/products/advieh.jpg",
            SortOrder = 2
        };
        var herbs = new StoreCategory
        {
            Name = "سبزی‌های خشک",
            Slug = "dried-herbs",
            Description = "سبزی خشک طبیعی برای آش، خورش و دمنوش.",
            ImageUrl = "/images/store/products/dried-mint.jpg",
            SortOrder = 3
        };
        var gifts = new StoreCategory
        {
            Name = "پک‌های هدیه",
            Slug = "gifts",
            Description = "ست‌های هدیهٔ ادویه با بسته‌بندی ویژه.",
            ImageUrl = "/images/store/banner-gift.jpg",
            SortOrder = 4
        };

        db.Set<StoreCategory>().AddRange(asli, blends, herbs, gifts);
        await db.SaveChangesAsync();

        string Img(string name) => $"/images/store/products/{name}.jpg";

        var products = new List<StoreProduct>
        {
            P("زعفران سرگل ممتاز", "saffron-sargol", asli.Id, 890_000, 790_000,
                "زعفران سرگل با رنگ و عطر بالا؛ مناسب برای پلو و دسر.",
                "زعفران سرگل ممتاز ادویه‌چین از بهترین مزارع ایران انتخاب می‌شود. رنگ‌دهی بالا، عطر ماندگار و بسته‌بندی محافظ از ویژگی‌های این محصول است. برای هر پرس پلو تنها به مقدار اندکی نیاز دارید.",
                "۱ گرم", 34, "SAF-1G", Img("saffron"), true, 4.9, 128),
            P("زعفران پوشال ممتاز", "saffron-poushal", asli.Id, 1_450_000, null,
                "زعفران پوشال با عطر قوی؛ انتخاب حرفه‌ای‌ها.",
                "زعفران پوشال با رشته‌های بلند و عطر بسیار قوی، برای رستوران‌ها و مصارف حرفه‌ای مناسب است. در بسته‌بندی دربسته و مقاوم به نور عرضه می‌شود.",
                "۲ گرم", 18, "SAF-2G", Img("saffron"), false, 4.8, 64),
            P("زردچوبهٔ هندی", "turmeric", asli.Id, 95_000, 82_000,
                "زردچوبهٔ آسیاب‌شده با رنگ طلایی و عطر تازه.",
                "زردچوبهٔ خالص هندی با رنگ طلایی و عطر گرم، مناسب برای خورش، برنج و نوشیدنی‌های گرم. آسیاب‌شده و بدون افزودنی.",
                "۱۰۰ گرم", 120, "TUR-100", Img("turmeric"), true, 4.7, 210),
            P("فلفل سیاه آسیاب‌شده", "black-pepper-ground", asli.Id, 145_000, null,
                "فلفل سیاه تازه آسیاب‌شده با تندی متعادل.",
                "فلفل سیاه دانه‌درشت تازه آسیاب‌شده؛ عطر تند و تازهٔ آن در آشپزی روزمره تفاوت ایجاد می‌کند.",
                "۱۰۰ گرم", 88, "PEP-G100", Img("black-pepper"), true, 4.6, 156),
            P("فلفل سیاه دانه‌ای", "black-pepper-whole", asli.Id, 165_000, null,
                "دانه‌های کامل فلفل سیاه برای آسیاب تازه.",
                "دانه‌های کامل و یکدست فلفل سیاه؛ با آسیاب کردن تازه، عطر و تندی بیشتری به غذا می‌دهید.",
                "۱۰۰ گرم", 72, "PEP-W100", Img("black-pepper"), false, 4.5, 91),
            P("دارچین سیلان", "cinnamon-ceylon", asli.Id, 180_000, 158_000,
                "دارچین سیلان با عطر شیرین و پوست نازک.",
                "دارچین سیلان اصل با پوست نازک و عطر شیرین، مناسب برای دسر، چای و خورش‌های ایرانی.",
                "۱۰۰ گرم", 60, "CIN-100", Img("cinnamon"), true, 4.8, 173),
            P("سماق قرمز", "sumac", asli.Id, 135_000, null,
                "سماق قرمز با ترشی طبیعی و رنگ چشم‌نواز.",
                "سماق قرمز خالص با ترشی طبیعی و رنگ زنده؛ همراه همیشگی کباب و سالاد.",
                "۱۵۰ گرم", 95, "SUM-150", Img("sumac"), false, 4.6, 84),
            P("نعناع خشک", "dried-mint", herbs.Id, 78_000, null,
                "نعناع خشک طبیعی با عطر تازه.",
                "برگ‌های نعناع خشک‌شده در سایه؛ عطر تازه و رنگ سبز طبیعی برای دمنوش، ماست و آش.",
                "۵۰ گرم", 140, "HRB-MIN", Img("dried-mint"), true, 4.7, 118),
            P("آویشن خشک", "dried-thyme", herbs.Id, 85_000, null,
                "آویشن کوهی خشک با عطر گرم.",
                "آویشن کوهی خشک‌شده در سایه، مناسب برای خورش، سوپ و دمنوش. بدون رنگ و افزودنی.",
                "۵۰ گرم", 110, "HRB-THY", Img("dried-mint"), false, 4.5, 57),
            P("شوید خشک", "dried-dill", herbs.Id, 72_000, null,
                "شوید خشک با رنگ سبز و عطر ماندگار.",
                "شوید خشک طبیعی برای آش، ماست‌وخیار و خورش. خشک‌شده در سایه برای حفظ عطر.",
                "۵۰ گرم", 130, "HRB-DIL", Img("dried-mint"), false, 4.4, 43),
            P("هل سبز", "green-cardamom", asli.Id, 265_000, 239_000,
                "هل سبز خوش‌عطر برای چای و دسر.",
                "دانه‌های کامل هل سبز با عطر شیرین و تازه؛ مناسب برای چای، قهوه و دسرهای ایرانی.",
                "۵۰ گرم", 48, "CAR-50", Img("cardamom"), true, 4.9, 201),
            P("ادویه پلویی", "advieh-polo", blends.Id, 120_000, 105_000,
                "ترکیب آمادهٔ ادویه برای پلوی ایرانی.",
                "ترکیب متعادل زیره، دارچین، هل، گل محمدی و ادویه‌های معطر برای پلوی ایرانی. بدون نمک و افزودنی.",
                "۱۰۰ گرم", 100, "BLD-POLO", Img("advieh"), true, 4.8, 143),
            P("ادویه کاری", "curry-powder", blends.Id, 98_000, null,
                "ادویه کاری گرم و خوش‌رنگ.",
                "ترکیب زردچوبه، گشنیز، زیره و فلفل برای غذاهای هندی و کاری. عطر گرم و طعم متعادل.",
                "۱۰۰ گرم", 86, "BLD-CURRY", Img("advieh"), false, 4.5, 68),
            P("ادویه مرغ", "advieh-chicken", blends.Id, 115_000, null,
                "ترکیب آماده برای مرغ و گوشت.",
                "ترکیب ادویه‌های معطر برای مزه‌دار کردن مرغ، گوشت و کباب. آمادهٔ استفاده و بدون نمک.",
                "۱۰۰ گرم", 92, "BLD-CHK", Img("advieh"), false, 4.6, 77),
            P("پک هدیهٔ ادویه", "spice-gift-box", gifts.Id, 1_250_000, 1_090_000,
                "ست هدیهٔ شش ادویه در جعبهٔ چوبی.",
                "ست هدیهٔ ادویه‌چین شامل شش شیشهٔ ادویهٔ منتخب در جعبهٔ چوبی با روبان؛ انتخاب مناسب برای هدیه.",
                "ست ۶ عددی", 25, "GFT-6", "/images/store/banner-gift.jpg", true, 4.9, 52),
            P("پک ادویهٔ هفت‌گانه", "seven-spice-box", gifts.Id, 780_000, 690_000,
                "هفت ادویهٔ پرکاربرد در یک پک.",
                "پک هفت‌گانهٔ ادویه‌های پرکاربرد آشپزخانهٔ ایرانی با بسته‌بندی شیشه‌ای و برچسب مشخص.",
                "ست ۷ عددی", 40, "GFT-7", "/images/store/banner-gift.jpg", false, 4.7, 38),
            P("گل محمدی خشک", "rose-petals", herbs.Id, 95_000, null,
                "گل محمدی خشک با عطر طبیعی.",
                "گلبرگ‌های گل محمدی خشک‌شده در سایه؛ برای دمنوش، دسر و ترکیب ادویه.",
                "۵۰ گرم", 105, "HRB-ROSE", Img("advieh"), false, 4.8, 66),
            P("زنجبیل خشک", "dried-ginger", asli.Id, 88_000, null,
                "زنجبیل خشک آسیاب‌شده با طعم گرم.",
                "زنجبیل خشک آسیاب‌شده با عطر تند و گرم؛ مناسب برای دمنوش، شیرینی و غذاهای آسیایی.",
                "۱۰۰ گرم", 76, "GIN-100", Img("turmeric"), false, 4.5, 49)
        };

        db.Set<StoreProduct>().AddRange(products);
        await db.SaveChangesAsync();
    }

    private static StoreProduct P(
        string name, string slug, int categoryId, long price, long? discount,
        string shortDescription, string description, string unit, int stockQuantity, string sku,
        string imageUrl, bool featured, double rating, int reviews) => new()
    {
        Name = name,
        Slug = slug,
        CategoryId = categoryId,
        ShortDescription = shortDescription,
        Description = description,
        PriceTomans = price,
        DiscountPriceTomans = discount,
        Unit = unit,
        StockQuantity = stockQuantity,
        Sku = sku,
        ImageUrl = imageUrl,
        IsActive = true,
        IsFeatured = featured,
        Rating = rating,
        ReviewCount = reviews
    };
}
