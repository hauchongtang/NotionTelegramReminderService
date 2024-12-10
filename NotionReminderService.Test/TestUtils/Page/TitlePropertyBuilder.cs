using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class TitlePropertyBuilder
{
    private string? _title;

    public TitlePropertyBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }
    
    public TitlePropertyValue Build()
    {
        return new TitlePropertyValue
        {
            Title =
            [
                new RichTextBase
                {
                    PlainText = _title
                }
            ]
        };
    }
}