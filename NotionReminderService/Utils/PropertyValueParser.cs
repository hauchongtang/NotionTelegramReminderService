using Notion.Client;

namespace NotionReminderService.Utils;

public static class PropertyValueParser<T> {
    public static T? GetValueFromPage(Page page, string keyValue)
    {
        if (!page.Properties.ContainsKey(keyValue)) return default;

        page.Properties.TryGetValue(keyValue, out var targetPropertyValue);
        return (T)Convert.ChangeType(targetPropertyValue, typeof(T))!;
    }
}