using BedBrigade.Common.Enums;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BedBrigade.Client.Swagger;

public class BedRequestStatusSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Type != typeof(BedRequestStatus) || schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        mutableSchema.Description =
            """
            Represents the current stage of a bed request.

            Values:
            * `1` - Waiting: The request is waiting to be scheduled.
            * `2` - Scheduled: The request has been scheduled for delivery.
            * `3` - Delivered: The requested beds were delivered to the recipient.
            * `4` - Given: The requested beds were given to the recipient without delivery.
            * `5` - Cancelled: The request was cancelled.
            """;
    }
}
