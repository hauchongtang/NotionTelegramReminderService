using Microsoft.Extensions.Options;
using Notion.Client;
using NotionReminderService.Config;

namespace NotionReminderService.Services.NotionHandlers.NotionService;

public class NotionService(INotionClient notionClient, IOptions<NotionConfiguration> notionConfig) : INotionService
{
    public async Task<PaginatedList<Page>> GetPaginatedList(DatabasesQueryParameters parameters)
    {
        return await notionClient.Databases.QueryAsync(notionConfig.Value.DatabaseId, parameters);
    }

    public async Task<List<Page>> UpdateEventsToCompleted(PaginatedList<Page> pages)
    {
        var updatedPages = new List<Page>();
        foreach (var page in pages.Results)
        {
            var eventStatusPropertyValue = GetNotionEventStatus(page);
            if (eventStatusPropertyValue is null) continue;
            
            SetStatusToDone(eventStatusPropertyValue);
            
            var updatedPage = await ExecuteUpdatePage(page, eventStatusPropertyValue);
            updatedPages.Add(updatedPage);
        }

        return updatedPages;
    }
    
    private static StatusPropertyValue? GetNotionEventStatus(Page page)
    {
        if (!page.Properties.ContainsKey("Status")) return null;

        page.Properties.TryGetValue("Status", out var statusPropValue);
        var status = ((StatusPropertyValue)statusPropValue!);
        return status;
    }

    private static void SetStatusToDone(StatusPropertyValue statusPropertyValue)
    {
        statusPropertyValue.Status.Id = null;
        statusPropertyValue.Status.Name = "Done";
        statusPropertyValue.Status.Color = StatusPropertyValue.StatusColor.Green;
    }
    
    private async Task<Page> ExecuteUpdatePage(Page page, StatusPropertyValue eventStatusPropertyValue)
    {
        var updatedPage = await notionClient.Pages.UpdateAsync(page.Id, new PagesUpdateParameters
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                { "Status", eventStatusPropertyValue }
            }
        });
        return updatedPage;
    }

    public async Task<List<Page>> UpdateEventsToInProgress(PaginatedList<Page> pages)
    {
        var updatedPages = new List<Page>();
        foreach (var page in pages.Results)
        {
            var eventStatusPropertyValue = GetNotionEventStatus(page);
            if (eventStatusPropertyValue is null) continue;
            
            SetStatusToInProgress(eventStatusPropertyValue);
            
            var updatedPage = await ExecuteUpdatePage(page, eventStatusPropertyValue);
            updatedPages.Add(updatedPage);
        }

        return updatedPages;
    }

    private static void SetStatusToInProgress(StatusPropertyValue statusPropertyValue)
    {
        statusPropertyValue.Status.Id = null;
        statusPropertyValue.Status.Name = "In progress";
        statusPropertyValue.Status.Color = StatusPropertyValue.StatusColor.Blue;
    }

    public async Task<Page> CreateNewEvent(PagesCreateParameters parameters)
    {
        var page = await notionClient.Pages.CreateAsync(parameters);
        return page;
    }
}