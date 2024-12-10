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
}