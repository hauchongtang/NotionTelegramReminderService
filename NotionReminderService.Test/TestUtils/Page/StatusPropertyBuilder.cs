using Notion.Client;

namespace NotionReminderService.Test.TestUtils.Page;

public class StatusPropertyBuilder
{
    private string? _status;
    
    public StatusPropertyBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }
    
    public StatusPropertyValue Build()
    {
        return new StatusPropertyValue
        {
            Status = new StatusPropertyValue.Data
            {
                Name = _status
            }
        };
    }
}