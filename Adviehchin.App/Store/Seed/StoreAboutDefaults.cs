using System.Text.Json;
using Adviehchin.App.Store.Entities;
using Adviehchin.App.Store.Models;

namespace Adviehchin.App.Store.Seed;

/// <summary>محتوای پیش‌فرض صفحهٔ درباره ما؛ برای اولین seed و fallback خواندن.</summary>
public static class StoreAboutDefaults
{
    public static StoreAboutPage Create() => new()
    {
        Title = "دربارهٔ ادویه‌چین",
        Lead = "چاشنی لحظه‌های خوشمزه؛ روایتی از عطر، اصالت و اعتماد — از حجرهٔ بازار تهران تا سفرهٔ شما.",
        StoryTitle = "از حجرهٔ بازار تا سفرهٔ شما",
        StoryBody = """
            ادویه‌چین از قلب بازار تهران، خیابان ۱۲ فروردین، آغاز شد؛ جایی که عطر هل و دارچین و زعفران، سال‌هاست رهگذران را به ایستادن وامی‌دارد. ما همان عطارِ محله‌ایم که این‌بار آنلاین شده تا دست شما هم به همان ادویهٔ تازه برسد.

            هر ادویه و سبزی خشک را مستقیم از کشاورزان و کارگاه‌های محلی دست‌چین می‌کنیم؛ آسیاب و بسته‌بندی هفتگی، بدون رنگ و افزودنی، تا عطر و طعم اصیل آشپزخانهٔ ایرانی همان‌طور که باید، به سفرهٔ شما برسد.

            باور ما ساده است: مشتری باید با خیال راحت خرید کند؛ پس اصالت و تازگی را تضمین می‌کنیم، هفت روز ضمانت بازگشت می‌دهیم و تا لحظهٔ تحویل، پاسخ‌گوی شما هستیم.
            """,
        ImageUrl = "/images/store/hero-spices.jpg",
        ValuesJson = JsonSerializer.Serialize(new List<AboutValueItem>
        {
            new() { Icon = "bi-patch-check", Title = "اصالت و تازگی", Text = "تهیهٔ مستقیم از کشاورزان و کارگاه‌های محلی؛ آسیاب و بسته‌بندی هفتگی بدون افزودنی." },
            new() { Icon = "bi-truck", Title = "ارسال سریع و مطمئن", Text = "ارسال به سراسر ایران؛ رایگان برای خرید بالای ۲٬۰۰۰٬۰۰۰ تومان." },
            new() { Icon = "bi-arrow-repeat", Title = "بازگشت آسان کالا", Text = "۷ روز ضمانت بازگشت در صورت نارضایتی؛ بدون پرسش‌های پیچیده." },
            new() { Icon = "bi-credit-card", Title = "پرداخت به انتخاب شما", Text = "پرداخت در محل یا آنلاین؛ هر طور که راحت‌ترید." }
        }),
        StatsJson = JsonSerializer.Serialize(new List<AboutStatItem>
        {
            new() { Value = "۳۰", Label = "ادویه و سبزی خشکِ دست‌چین" },
            new() { Value = "۷ روز", Label = "ضمانت بازگشت کالا" },
            new() { Value = "۹ تا ۲۱", Label = "پاسخ‌گویی همه‌روزه" }
        }),
        UpdatedAt = DateTime.Now
    };
}