using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class MultiSelectPropertyBuilder
{
    private readonly List<SelectOption> _selectOptions = [];

    public MultiSelectPropertyBuilder WithSelectOption(SelectOption selectOption)
    {
        _selectOptions.Add(selectOption);
        return this;
    }
    
    public MultiSelectPropertyValue Build()
    {
        return new MultiSelectPropertyValue
        {
            MultiSelect = _selectOptions
        };
    }
}