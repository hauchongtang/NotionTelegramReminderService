public class PropertyValueParserTest
{
    private Page _pageWithProperties;

    [Setup]
    public void Setup()
    {
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

        Assert.IsInstanceOf<TitlePropertyValue(titlePropertyValue);
    }

    [Test]
    public void ParseDatePropertyValue()
    {
        var datePropertyValue = PropertyValueParser<DatePropertyValue>.GetValueFromPage(_pageWithProperties, "Date");

        Assert.IsInstanceOf<DatePropertyValue>(datePropertyValue);
    }

    [Test]
    public void ParseRichTextPropertyBuilder()
    {
        var richTextPropertyBuilder = PropertyValueParser<RichTextPropertyValue>.GetValueFromPage(_pageWithProperties, "Location");

        Assert.IsInstanceOf<RichTextPropertyValue>(richTextPropertyValue);
    }
}