using System.Net;
using Adviehchin.App.Store.Sync;

namespace Adviehchin.Tests;

/// <summary>استخراج متن و گزینه‌های محصول از HTML منبع، بدون نیاز به شبکه.</summary>
public class StoreSourceParsingTests
{
    private static readonly IReadOnlyList<SourceAttribute> Attributes =
    [
        new("بسته بندی", "pa_packaging", [new SourceTerm("سلفون", "cellophane")]),
        new("وزن", "pa_weight", [new SourceTerm("100 گرم", "100g"), new SourceTerm("200 گرم", "200g")])
    ];

    [Fact]
    public void ParseVariations_ReadsEncodedJsonAndPersianOptionNames()
    {
        const string json = """
        [{"variation_id":1557,"sku":"BRP-01-14","display_price":250000,
          "display_regular_price":250000,"is_in_stock":true,
          "variation_is_active":true,"variation_is_visible":true,
          "attributes":{"attribute_pa_packaging":"cellophane","attribute_pa_weight":"100g"}}]
        """;

        var html = $"<form data-product_variations=\"{WebUtility.HtmlEncode(json)}\"></form>";

        var variations = StoreSourceParsing.ParseVariations(html, Attributes);

        var variation = Assert.Single(variations);
        Assert.Equal(1557, variation.ExternalId);
        Assert.Equal("BRP-01-14", variation.Sku);
        Assert.Equal(250_000, variation.PriceTomans);
        Assert.Null(variation.DiscountPriceTomans);
        Assert.True(variation.InStock);
        Assert.True(variation.IsActive);
        Assert.Equal("سلفون / 100 گرم", variation.Title);
        Assert.Contains("بسته بندی", variation.OptionsJson);
        Assert.Contains("وزن", variation.OptionsJson);
    }

    [Fact]
    public void ParseVariations_MarksSaleAndInactiveOption()
    {
        const string json = """
        [{"variation_id":9,"display_price":180000,"display_regular_price":250000,
          "is_in_stock":false,"variation_is_active":true,"variation_is_visible":false,
          "attributes":{"attribute_pa_weight":"200g"}}]
        """;

        var html = $"<div data-product_variations='{WebUtility.HtmlEncode(json)}'></div>";

        var variation = Assert.Single(StoreSourceParsing.ParseVariations(html, Attributes));

        Assert.Equal(180_000, variation.DiscountPriceTomans);
        Assert.False(variation.InStock);
        Assert.False(variation.IsActive);
        Assert.Equal("200 گرم", variation.Title);
    }

    [Fact]
    public void ParseVariations_ReturnsEmptyWhenPageHasNoVariationData()
    {
        Assert.Empty(StoreSourceParsing.ParseVariations("<html><body>محصول ساده</body></html>", Attributes));
    }

    [Fact]
    public void HtmlToPlainText_StripsMarkupAndDecodesEntities()
    {
        const string html = "<p>ادویه <strong>پاپریکا</strong> ایرانی &#8211; 100 گرمی</p>";

        var text = StoreSourceParsing.HtmlToPlainText(html, 500);

        Assert.Equal("ادویه پاپریکا ایرانی – 100 گرمی", text);
    }

    [Fact]
    public void HtmlToPlainText_TruncatesToMaximumLength()
    {
        var text = StoreSourceParsing.HtmlToPlainText(new string('ا', 800), 500);

        Assert.NotNull(text);
        Assert.True(text!.Length <= 500);
        Assert.EndsWith("…", text);
    }

    [Fact]
    public void DecodeName_NormalizesSpacingAndEntities()
    {
        Assert.Equal("ادویه پاپریکا ایرانی – 100 گرمی",
            StoreSourceParsing.DecodeName("  ادویه   پاپریکا ایرانی &#8211; 100 گرمی ", 200));
    }
}
