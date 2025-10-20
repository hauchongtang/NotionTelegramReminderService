using Newtonsoft.Json;

namespace NotionReminderService.Test.TestData;

public static class TestDataUtils
{
    public const string RainfallResponseFilePath = "TestData/Rainfall/hasRainfall.json";
    public const string NoRainfallResponseFilePath = "TestData/Rainfall/noRainfall.json";
    
    public static T LoadTestDataFromFile<T>(string filePath)
    {
        var testDir = TestContext.CurrentContext.TestDirectory;
        filePath = filePath.Replace("/", Path.DirectorySeparatorChar.ToString());
        testDir = testDir.Replace("bin/Debug/net9.0", "");
        var fullPath = Path.Combine(testDir, filePath);
        var jsonData = File.ReadAllText(fullPath);
        return JsonConvert.DeserializeObject<T>(jsonData)!;
    }
}