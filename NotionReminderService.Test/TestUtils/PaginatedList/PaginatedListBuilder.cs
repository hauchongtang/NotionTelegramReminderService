using Notion.Client;

namespace NotionReminderService.Test.TestUtils.PaginatedList;

public class PaginatedListBuilder
{
    private readonly PaginatedList<Notion.Client.Page> _paginatedList = new()
    {
        Results = []
    };

    public PaginatedListBuilder AddNewPage(Notion.Client.Page page)
    {
        _paginatedList.Results.Add(page);
        return this;
    }

    public PaginatedList<Notion.Client.Page> Build() {
        return _paginatedList;
    }
}