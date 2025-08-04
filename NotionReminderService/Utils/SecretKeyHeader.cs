using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NotionReminderService.Config;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NotionReminderService.Utils;

public class SecretKeyHeader(IOptions<BotConfiguration> botConfig) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
        {
            operation.Parameters = new List<OpenApiParameter>();
        }
        operation.Parameters.Add(new  OpenApiParameter
        {
            Name = botConfig.Value.SecretTokenHeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        });
    }
}