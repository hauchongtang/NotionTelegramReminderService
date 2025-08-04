namespace NotionReminderService.Utils;

public static class BusUtil
{
    public static Dictionary<string, string> BusType = new()
    {
        { "SD", "Single" },
        { "DD", "Double" },
        { "BD", "Bendy" },
    };
    
    public static string GetBusArrivalTimeSpan(DateTime arrivalTime, DateTime timeNow)
    {
        var timeSpan = arrivalTime - timeNow;
        var minutes = (int)timeSpan.TotalMinutes;
        if (minutes == 0) return "Arr";
        return $"{arrivalTime.ToString("h:mm tt")}";
    }
}