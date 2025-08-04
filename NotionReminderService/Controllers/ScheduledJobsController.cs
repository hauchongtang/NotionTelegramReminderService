using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotionReminderService.Services.BotHandlers.TransportHandler;
using NotionReminderService.Utils;

namespace NotionReminderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ScheduledJobsController(
    ILogger<ScheduledJobsController> logger,
    ITransportService transportService)
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
}