using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class NotionPageBuilder
{
    private readonly IDictionary<string, PropertyValue> _properties = new Dictionary<string, PropertyValue>();

    public NotionPageBuilder WithProperty(string key, PropertyValue value)
    {
        _properties.Add(key, value);
        return this;
    }
    
    public Notion.Client.Page Build()
    {
        return new Notion.Client.Page
        {
            Properties = _properties
        };
    }
}