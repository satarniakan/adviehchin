using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Adviehchin.App.Store.Entities;
using Adviehchin.Application.Helpers;

namespace Adviehchin.App.Store.Models;

/// <summary>یک خط سبد خرید؛ در localStorage مرورگر نگه‌داری می‌شود.</summary>
public class CartLine
{
    public int ProductId { get; set; }

    /// <summary>گزینهٔ انتخاب‌شده؛ null برای محصول ساده.</summary>
    public int? VariantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>برای محصول چندگزینه‌ای، عنوان گزینه (مثل «سلفون / ۱۰۰ گرم»).</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>کلید نوع بسته‌بندی انتخابی مشتری (معمولی/آمیرزا/شاهپسند).</summary>
    public string Packaging { get; set; } = StorePackaging.DefaultKey;

    public string? ImageUrl { get; set; }
    public long UnitPriceTomans { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }

    public long LineTotalTomans => UnitPriceTomans * Quantity;
}

public class CheckoutModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Note { get; set; }
    public StorePaymentMethod PaymentMethod { get; set; } = StorePaymentMethod.CashOnDelivery;
}

public class StoreQuery
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}

public class ProductAdminInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است.")]
    [StringLength(200, ErrorMessage = "نام حداکثر ۲۰۰ نویسه است.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "نشانی حداکثر ۲۰۰ نویسه است.")]
    public string Slug { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "دسته‌بندی را انتخاب کنید.")]
    public int CategoryId { get; set; }

    [StringLength(500, ErrorMessage = "توضیح کوتاه حداکثر ۵۰۰ نویسه است.")]
    public string? ShortDescription { get; set; }

    [StringLength(4000, ErrorMessage = "توضیحات حداکثر ۴۰۰۰ نویسه است.")]
    public string? Description { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "قیمت باید بزرگ‌تر از صفر باشد.")]
    public long PriceTomans { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "قیمت با تخفیف باید مثبت باشد.")]
    public long? DiscountPriceTomans { get; set; }

    [Required(ErrorMessage = "واحد فروش الزامی است.")]
    [StringLength(50, ErrorMessage = "واحد فروش حداکثر ۵۰ نویسه است.")]
    public string Unit { get; set; } = "۱۰۰ گرم";

    [Range(0, 100000, ErrorMessage = "موجودی باید بین ۰ تا ۱۰۰٬۰۰۰ باشد.")]
    public int StockQuantity { get; set; }

    [StringLength(64, ErrorMessage = "SKU حداکثر ۶۴ نویسه است.")]
    public string? Sku { get; set; }

    [StringLength(500, ErrorMessage = "نشانی تصویر حداکثر ۵۰۰ نویسه است.")]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
}

public class CategoryAdminInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته الزامی است.")]
    [StringLength(150, ErrorMessage = "نام حداکثر ۱۵۰ نویسه است.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(160, ErrorMessage = "نشانی حداکثر ۱۶۰ نویسه است.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "توضیحات حداکثر ۲۰۰۰ نویسه است.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "نشانی تصویر حداکثر ۵۰۰ نویسه است.")]
    public string? ImageUrl { get; set; }

    [Range(0, 1000, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۱۰۰۰ باشد.")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AboutValueItem
{
    public string Icon { get; set; } = "bi-patch-check";

    [Required(ErrorMessage = "عنوان ارزش الزامی است.")]
    [StringLength(100, ErrorMessage = "عنوان ارزش حداکثر ۱۰۰ نویسه است.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن ارزش الزامی است.")]
    [StringLength(300, ErrorMessage = "متن ارزش حداکثر ۳۰۰ نویسه است.")]
    public string Text { get; set; } = string.Empty;
}

public class AboutStatItem
{
    [Required(ErrorMessage = "مقدار آمار الزامی است.")]
    [StringLength(30, ErrorMessage = "مقدار آمار حداکثر ۳۰ نویسه است.")]
    public string Value { get; set; } = string.Empty;

    [Required(ErrorMessage = "برچسب آمار الزامی است.")]
    [StringLength(100, ErrorMessage = "برچسب آمار حداکثر ۱۰۰ نویسه است.")]
    public string Label { get; set; } = string.Empty;
}

public class AboutAdminInput
{
    [Required(ErrorMessage = "عنوان صفحه الزامی است.")]
    [StringLength(150, ErrorMessage = "عنوان حداکثر ۱۵۰ نویسه است.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن سربرگ الزامی است.")]
    [StringLength(600, ErrorMessage = "متن سربرگ حداکثر ۶۰۰ نویسه است.")]
    public string Lead { get; set; } = string.Empty;

    [Required(ErrorMessage = "عنوان داستان الزامی است.")]
    [StringLength(150, ErrorMessage = "عنوان داستان حداکثر ۱۵۰ نویسه است.")]
    public string StoryTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن داستان الزامی است.")]
    [StringLength(8000, ErrorMessage = "متن داستان حداکثر ۸۰۰۰ نویسه است.")]
    public string StoryBody { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "نشانی تصویر حداکثر ۵۰۰ نویسه است.")]
    public string? ImageUrl { get; set; }

    public List<AboutValueItem> Values { get; set; } = [];
    public List<AboutStatItem> Stats { get; set; } = [];

    public DateTime UpdatedAt { get; set; }
}

/// <summary>محتوای خواندنی صفحهٔ درباره ما برای نمایش عمومی.</summary>
public class AboutContent
{
    public string Title { get; set; } = string.Empty;
    public string Lead { get; set; } = string.Empty;
    public string StoryTitle { get; set; } = string.Empty;
    public string StoryBody { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public IReadOnlyList<AboutValueItem> Values { get; set; } = [];
    public IReadOnlyList<AboutStatItem> Stats { get; set; } = [];
}

public static class AboutContentMapper
{
    public static AboutContent ToContent(this StoreAboutPage row) => new()
    {
        Title = row.Title,
        Lead = row.Lead,
        StoryTitle = row.StoryTitle,
        StoryBody = row.StoryBody,
        ImageUrl = row.ImageUrl,
        Values = JsonSerializer.Deserialize<List<AboutValueItem>>(row.ValuesJson) ?? [],
        Stats = JsonSerializer.Deserialize<List<AboutStatItem>>(row.StatsJson) ?? []
    };

    public static AboutAdminInput ToInput(this StoreAboutPage row) => new()
    {
        Title = row.Title,
        Lead = row.Lead,
        StoryTitle = row.StoryTitle,
        StoryBody = row.StoryBody,
        ImageUrl = row.ImageUrl,
        Values = JsonSerializer.Deserialize<List<AboutValueItem>>(row.ValuesJson) ?? [],
        Stats = JsonSerializer.Deserialize<List<AboutStatItem>>(row.StatsJson) ?? [],
        UpdatedAt = row.UpdatedAt
    };
}

public static class AboutIcons
{
    public static readonly (string Key, string Label)[] All =
    [
        ("bi-patch-check", "نشان اصالت"),
        ("bi-stars", "تازگی و درخشش"),
        ("bi-truck", "ارسال"),
        ("bi-arrow-repeat", "بازگشت کالا"),
        ("bi-credit-card", "پرداخت"),
        ("bi-award", "کیفیت"),
        ("bi-shield-check", "اعتماد"),
        ("bi-heart", "مشتری‌مداری"),
        ("bi-shop", "بازار و حجره"),
        ("bi-gift", "هدیه"),
        ("bi-telephone", "پاسخ‌گویی"),
        ("bi-geo-alt", "موقعیت")
    ];
}

public static class StoreText
{
    public static string Price(long tomans) => PersianDigits(CurrencyFormatter.ToToman(tomans));

    public static string PersianDigits(string value) =>
        string.Concat(value.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c));

    public static string ToPersian(this StoreOrderStatus status) => status switch
    {
        StoreOrderStatus.PendingPayment => "در انتظار پرداخت",
        StoreOrderStatus.Paid => "پرداخت‌شده",
        StoreOrderStatus.Processing => "در حال آماده‌سازی",
        StoreOrderStatus.Shipped => "ارسال‌شده",
        StoreOrderStatus.Delivered => "تحویل‌شده",
        StoreOrderStatus.Cancelled => "لغوشده",
        _ => status.ToString()
    };

    public static string BadgeClass(this StoreOrderStatus status) => status switch
    {
        StoreOrderStatus.PendingPayment => "text-bg-warning",
        StoreOrderStatus.Paid => "text-bg-success",
        StoreOrderStatus.Processing => "text-bg-info",
        StoreOrderStatus.Shipped => "text-bg-primary",
        StoreOrderStatus.Delivered => "text-bg-success",
        StoreOrderStatus.Cancelled => "text-bg-secondary",
        _ => "text-bg-secondary"
    };

    public static string ToPersian(this StorePaymentMethod method) => method switch
    {
        StorePaymentMethod.CashOnDelivery => "پرداخت در محل",
        StorePaymentMethod.OnlineGateway => "پرداخت آنلاین (آزمایشی)",
        _ => method.ToString()
    };

    public static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant()
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Replace(' ', '-');
        var chars = slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    public static readonly string[] Provinces =
    [
        "تهران", "البرز", "اصفهان", "خراسان رضوی", "فارس", "آذربایجان شرقی", "آذربایجان غربی",
        "خوزستان", "گیلان", "مازندران", "کرمان", "قم", "یزد", "کرمانشاه", "گلستان", "هرمزگان",
        "سیستان و بلوچستان", "همدان", "مرکزی", "قزوین", "زنجان", "اردبیل", "کردستان", "لرستان",
        "بوشهر", "چهارمحال و بختیاری", "سمنان", "خراسان شمالی", "خراسان جنوبی", "ایلام", "کهگیلویه و بویراحمد"
    ];
}
