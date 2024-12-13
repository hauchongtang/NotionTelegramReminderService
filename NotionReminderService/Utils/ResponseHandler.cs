using Newtonsoft.Json;

namespace NotionReminderService.Utils;

public static class ResponseHandler
{
    public static async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(content);
        }

        return JsonConvert.DeserializeObject<T>(content) ??
               throw new Exception("Cannot deserialize response from API");
    }
}