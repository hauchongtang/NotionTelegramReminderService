using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using NotionReminderService.Config;

namespace NotionReminderService.Utils;

public class SecretKeyValidationAttribute(IOptions<BotConfiguration> botConfig) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var expectedSecretKey = botConfig.Value.SecretToken;
        var secretKeyInHeader = context.HttpContext.Request.Headers.TryGetValue(botConfig.Value.SecretTokenHeaderName,
            out var providedSecretKey);
        if (!secretKeyInHeader)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        if (string.IsNullOrEmpty(expectedSecretKey) || !providedSecretKey.ToString().Equals(expectedSecretKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        base.OnActionExecuting(context);
    }
}