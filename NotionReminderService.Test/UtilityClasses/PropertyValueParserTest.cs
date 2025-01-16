using Notion.Client;
using NotionReminderService.Test.TestUtils;
using NotionReminderService.Test.TestUtils.Page;
using NotionReminderService.Utils;

namespace NotionReminderService.Test.UtilityClasses;

public class PropertyValueParserTest
{
    private Page _pageWithProperties;

    [SetUp]
    public void Setup()
    {
        var firstDec2024 = new DateTimeBuilder().WithYear(2024).WithMonth(12).WithDay(1).Build();
        _pageWithProperties = new Page
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                { "Name", new TitlePropertyBuilder().WithTitle("Event 1").Build() },
                { "Date", new DatePropertyBuilder().WithStartDt(firstDec2024).Build() },
                { "Location", new RichTextPropertyBuilder().WithText("Singapore").Build() }
            }
        };
    }

    [Test]
    public void ParseTitlePropertyValue()
    {
        var titlePropertyValue = PropertyValueParser<TitlePropertyValue>.GetValueFromPage(_pageWithProperties, "Name");

        Assert.That(titlePropertyValue, Is.InstanceOf<TitlePropertyValue>());
    }

    [Test]
    public void ParseDatePropertyValue()
    {
        var datePropertyValue = PropertyValueParser<DatePropertyValue>.GetValueFromPage(_pageWithProperties, "Date");

        Assert.That(datePropertyValue, Is.InstanceOf<DatePropertyValue>());
    }

    [Test]
    public void ParseRichTextPropertyBuilder()
    {
        var richTextPropertyBuilder = PropertyValueParser<RichTextPropertyValue>.GetValueFromPage(_pageWithProperties, "Location");

        Assert.That(richTextPropertyBuilder, Is.InstanceOf<RichTextPropertyValue>());
    }
}