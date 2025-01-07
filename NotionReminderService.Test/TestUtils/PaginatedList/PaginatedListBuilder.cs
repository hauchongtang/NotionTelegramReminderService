public class PaginatedListBuilder
{
    PagainatedList<Page> paginatedList = new PagainatedList<Page>();

    public PaginatedListBuilder AddNewPage(Page page)
    {
        paginatedList.Results.Add(page);
        return this;
    }

    public PaginatedList<Page> Build() {
        return paginatedList;
    }
}