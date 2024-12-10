using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class RichTextPropertyBuilder
{
    private string? _text;
    
    public RichTextPropertyBuilder WithText(string? text)
    {
        _text = text;
        return this;
    }
    
    public RichTextPropertyValue Build()
    {
        return new RichTextPropertyValue
        {
            RichText =
            [
                new RichTextBase()
                {
                    PlainText = _text
                }
            ]
        };
    }
}