using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class SelectPropertyBuilder
{
    private string _id = "123";
    private SelectOption? _selectOption;

    public SelectPropertyBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public SelectPropertyBuilder WithSelectOption(SelectOption? selectOption)
    {
        _selectOption = selectOption;
        return this;
    }
    
    public SelectPropertyValue Build()
    {
        return new SelectPropertyValue
        {
            Id = _id,
            Select = _selectOption
        };
    }
}