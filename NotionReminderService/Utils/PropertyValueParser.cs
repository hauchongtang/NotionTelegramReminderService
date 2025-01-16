public static class PropertyValueParser<T> {
    public static T GetValueFromPage(Page page, string keyValue)
    {
        if (!page.Properties.ContainsKey(keyValue)) return null;

        page.Properties.TryGetValue(keyValue, out var targetPropertyValue);
        return (T)Convert.ChangeType(targetPropertyValue, typeof(T));
    }
}