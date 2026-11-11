namespace Adviehchin.App.Store.Entities;

/// <summary>محتوای صفحهٔ «درباره ما»؛ یک ردیف ثابت که ادمین ویرایش می‌کند.</summary>
public class StoreAboutPage
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Lead { get; set; } = string.Empty;
    public string StoryTitle { get; set; } = string.Empty;
    public string StoryBody { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ValuesJson { get; set; } = "[]";
    public string StatsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }
}