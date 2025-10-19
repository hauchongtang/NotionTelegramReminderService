using Microsoft.Extensions.Options;
using NotionReminderService.Api.GoogleAi;
using NotionReminderService.Api.Weather;
using NotionReminderService.Config;
using NotionReminderService.Models.Weather.Rainfall;
using NotionReminderService.Repositories.Weather;
using NotionReminderService.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotionReminderService.Services.BotHandlers.WeatherHandler;

public class WeatherMessageService(
    IWeatherApi weatherApi,
    IGoogleAiApi googleAiApi,
    ITelegramBotClient botClient,
    IWeatherRepository weatherRepository,
    IDateTimeProvider dateTimeProvider,
    IOptions<BotConfiguration> botConfig,
    ILogger<IWeatherMessageService> logger) : IWeatherMessageService
{
    public async Task UpdateRainfallStations()
    {
        var rainfallResponse = await weatherApi.GetRealTimeRainfallByLocation();
        var stations = rainfallResponse.Data.Stations.Select(x => new RainfallStationCached
        {
            Id = x.Id,
            DeviceId = x.DeviceId,
            Name = x.Name,
            Latitude = x.LabelLocation.Latitude,
            Longitude = x.LabelLocation.Longitude
        }).ToList();
        await weatherRepository.UpdateRainfallStations(stations);
    }

    public async Task<string?> CreateCurrentDayRainfallIfNotExists()
    {
        var currentDate = dateTimeProvider.Now.Date;
        var existingCurrentDayRainfall = await weatherRepository.GetRainfallByDateTime(currentDate);
        var rainfallId = existingCurrentDayRainfall?.RainfallId;
        if (existingCurrentDayRainfall is null)
        {
            rainfallId = await weatherRepository.CreateRainfall(currentDate);
        }
        return rainfallId;
    }
    
    public async Task UpdateRainfallReadings()
    {
        var rainfallResponse = await weatherApi.GetRealTimeRainfallByLocation();
        var currentDayRailFall = await weatherRepository.GetRainfallByDateTime(dateTimeProvider.Now.Date);
        var rainfallId = currentDayRailFall?.RainfallId;
        if (currentDayRailFall is null)
        {
            rainfallId = await CreateCurrentDayRainfallIfNotExists();
        }
        var dateTimeOffset = DateTimeOffset.Parse(rainfallResponse.Data.Readings[0].Timestamp);
        var dateTimeNow = dateTimeOffset.DateTime;
        var slotNumber = dateTimeNow.Minute < 30 ? 1 : 2;
        var slotNumberBasedOnSystemTime = dateTimeProvider.Now.Minute < 30 ? 1 : 2;
        if (slotNumber != slotNumberBasedOnSystemTime)
        {
            logger.LogWarning(
                $"Mismatch in slot number calculation. API SlotNumber: {slotNumber}, System SlotNumber: {slotNumberBasedOnSystemTime}");
            return;
        }
        var existingSlots = await weatherRepository.GetRainFallSlots(rainfallId!, slotNumber, dateTimeNow.Hour);
        var aggregatedReadings = rainfallResponse.Data.Readings[0].Data
            .GroupBy(x => x.StationId)
            .Select(g =>
            {
                var existingReading =
                    existingSlots.FirstOrDefault(x => x.StationId == g.Key && x.HourOfDay == dateTimeNow.Hour);
                var currentValue = g.First().Value;
                var aggregatedValue = existingReading?.RainfallAmount + currentValue ?? currentValue;
                return new RainfallSlot
                {
                    RainfallSlotId = existingReading?.RainfallSlotId ?? Guid.NewGuid().ToString(),
                    RainfallId = rainfallId!,
                    StationId = g.Key,
                    HourOfDay = dateTimeProvider.Now.Hour,
                    SlotNumber = slotNumber,
                    RainfallAmount = aggregatedValue,
                    UpdatedOn = dateTimeProvider.Now
                };
            });
        await weatherRepository.UpsertRainfallSlots(aggregatedReadings.ToList());
        var hourToRemove = -1;
        if (dateTimeNow.Hour >= 2)
        {
            hourToRemove = dateTimeNow.Hour - 2;
        }
        else
        {
            hourToRemove = 22 + dateTimeNow.Hour;
        }
        await weatherRepository.RemoveRainfallSlots(hourToRemove);
    }
    
    public async Task<RainfallSummary?> GetRainfallSummaryLastHour()
    {
        var rainfallSlots = await weatherRepository.GetRainfallSlotsLastHour();
        if (rainfallSlots is null)
        {
            throw new Exception($"Error in {nameof(GetRainfallSummaryLastHour)} -> No rainfall slots found for the last hour.");
        }

        var slots = rainfallSlots.ToList();
        var stationNameMap = await weatherRepository.GetRainfallStationsFromList(
            slots.Select(x => x.StationId).ToList());
        var rainfallLastHour = slots
            .GroupBy(x => x.StationId)
            .Select(g => new HourlyRainfall
            {
                StationId = g.Key,
                Amount = g.Sum(x => x.RainfallAmount),
                Location = stationNameMap[g.Key]
            }).ToList();
        
        var rainfallIntensitySettings = await weatherRepository.GetRainfallIntensitySettings();
        if (rainfallIntensitySettings is null)
        {
            throw new Exception($"Error in {nameof(GetRainfallSummaryLastHour)} -> No rainfall intensity settings found.");
        }

        var rainfallSummary = new RainfallSummary();
        foreach (var reading in rainfallLastHour)
        {
            if (reading.Amount <= 0.0)
            {
                rainfallSummary.NoRainfallStations.Add(reading.Location);
            }
            else if (reading.Amount > 0.0 && reading.Amount <= rainfallIntensitySettings.LowerBound)
            {
                rainfallSummary.LightRainfallStations.Add(reading.Location);
            }
            else if (reading.Amount > rainfallIntensitySettings.LowerBound &&
                     reading.Amount <= rainfallIntensitySettings.UpperBound)
            {
                rainfallSummary.ModerateRainfallStations.Add(reading.Location);
            }
            else
            {
                rainfallSummary.HeavyRainfallStations.Add(reading.Location);
            }
        }
        return rainfallSummary;
    }
    
    public async Task SendRainfallSummaryMessage(Chat? chat)
    {
        var rainfallSummary = await GetRainfallSummaryLastHour();
        if (rainfallSummary is null) return;
        var textToSend = "";
        var totalStations = rainfallSummary.NoRainfallStations.Count +
                            rainfallSummary.LightRainfallStations.Count +
                            rainfallSummary.ModerateRainfallStations.Count +
                            rainfallSummary.HeavyRainfallStations.Count;
        var noRainText = rainfallSummary.NoRainfallStations.Count == totalStations
            ? "All stations reported no rainfall in the last hour. ☀️"
            : $"No Rain ☀️({rainfallSummary.NoRainfallStations.Count}): {string.Join(", ", rainfallSummary.NoRainfallStations.OrderByDescending(x => x))}";
        var lightRainText = rainfallSummary.LightRainfallStations.Count == totalStations
            ? "All stations reported light rainfall in the last hour. 🌦️"
            : $"Light Rain 🌦️({rainfallSummary.LightRainfallStations.Count}): {string.Join(", ", rainfallSummary.LightRainfallStations.OrderByDescending(x => x))}";
        var moderateRainText = rainfallSummary.ModerateRainfallStations.Count == totalStations
            ? "All stations reported moderate rainfall in the last hour. 🌧️"
            : $"Moderate Rain 🌧️({rainfallSummary.ModerateRainfallStations.Count}): {string.Join(", ", rainfallSummary.ModerateRainfallStations.OrderByDescending(x => x))}";
        var heavyRainText = rainfallSummary.HeavyRainfallStations.Count == totalStations
            ? "All stations reported heavy rainfall in the last hour. ⛈️"
            : $"Heavy Rain ⛈️({rainfallSummary.HeavyRainfallStations.Count}): {string.Join(", ", rainfallSummary.HeavyRainfallStations.OrderByDescending(x => x))}";
        textToSend += $"""
                       <b>Rainfall Summary for the Last Hour (Since {dateTimeProvider.Now.Hour}:00):</b>
                       Light: >= 2.5mm/hr, Moderate: 2.5mm/hr - 7.5mm/hr, Heavy: > 7.5mm/hr
                       
                       {noRainText}
                       
                       {lightRainText}
                       
                       {moderateRainText}
                       
                       {heavyRainText}
                       """;
        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData("Retrieve Again", "triggerRainfallSummaryLastHour"))
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData("Get Weather Forecast", "triggerForecast"));
        await botClient.SendMessage(chat ?? new ChatId(botConfig.Value.ChatId), textToSend, ParseMode.Html, replyMarkup: replyMarkup);
    }
    
    public async Task SendMessage(Chat? chat)
    {
        var weatherData = await weatherApi.GetRealTimeWeather();

        var forecasts = weatherData?[0].Forecasts;
        if (forecasts is null) return;
        
        var jsonString = "{ data: [";
        foreach (var areaForecast in forecasts)
        {
            jsonString += "{" + $"{areaForecast.Area} : {areaForecast.Forecast}" + "},";
        }
        jsonString += "]}";
        
        // API call to LLM Model to summarize weather forecast
        var weatherSummaryResponse = await googleAiApi.GenerateContent(
            "Summarise this weather forecast in 60 words like a weatherman with some creativity. " +
            "List locations with rainy conditions if any. " +
            "If majority of locations are rainy, then list only those dry locations: \n" +
            jsonString);
        var weatherText = weatherSummaryResponse.Candidates[0].Content.Parts[0].Text;
        var textToSend = $"""
                          {weatherText}
                          
                          <i>Powered by Gemini LLM agent using real time data from <a href="https://data.gov.sg">data.gov.sg</a></i>
                          Forecast for → {weatherData?[0].ValidPeriod.Text}
                          """;

        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
            .AddButton(InlineKeyboardButton.WithCallbackData("Retrieve Again", "triggerForecast"))
            .AddNewRow()
            .AddButton(
                InlineKeyboardButton.WithCallbackData("Get Rainfall (Last 1h)", "triggerRainfallSummaryLastHour"));
        await botClient.SendMessage(chat ?? new ChatId(botConfig.Value.ChatId), textToSend, ParseMode.Html,
            replyMarkup: replyMarkup);
    }
}