using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionReminderService.Services.BotHandlers.TransportHandler;
using NotionReminderService.Services.BotHandlers.WeatherHandler;
using NotionReminderService.Utils;

namespace NotionReminderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ScheduledJobsController(
    ILogger<ScheduledJobsController> logger,
    ITransportService transportService, 
    IWeatherMessageService weatherMessageService)
    : ControllerBase
{
    [HttpPatch(nameof(UpdateBusStops))]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateBusStops()
    {
        try
        {
            await transportService.UpdateBusStops();
        }
        catch (Exception e)
        {
            logger.LogError($"{nameof(ScheduledJobsController)}.{nameof(UpdateBusStops)} failed. {e.Message})");
            return BadRequest(e.Message);
        }
        return Ok();
    }
    
    [HttpPatch(nameof(UpdateRainfallStations))]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateRainfallStations()
    {
        try
        {
            await weatherMessageService.UpdateRainfallStations();
        }
        catch (Exception e)
        {
            logger.LogError($"{nameof(ScheduledJobsController)}.{nameof(UpdateRainfallStations)} failed. {e.Message})");
            return BadRequest(e.Message);
        }
        return Ok();
    }

    [HttpPatch(nameof(UpdateRainfallReadings))]
    [ServiceFilter(typeof(SecretKeyValidationAttribute))]
    public async Task<IActionResult> UpdateRainfallReadings()
    {
        try
        {
            await weatherMessageService.UpdateRainfallReadings();
        }
        catch (Exception e)
        {
            logger.LogError($"{nameof(ScheduledJobsController)}.{nameof(UpdateRainfallReadings)} failed. {e.Message})");
            return BadRequest(e.Message);
        }
        return Ok();
    }
}