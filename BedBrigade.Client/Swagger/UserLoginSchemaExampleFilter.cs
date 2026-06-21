using BedBrigade.Common.Models;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace BedBrigade.Client.Swagger;

public class UserLoginSchemaExampleFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(UserLogin))
        {
            return;
        }

        if (schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        mutableSchema.Example = JsonNode.Parse("""
            {
              "email": "someone@somedomain.com",
              "password": "secret"
            }
            """);

        if (mutableSchema.Properties != null
            && mutableSchema.Properties.TryGetValue("email", out IOpenApiSchema? emailSchema)
            && emailSchema is OpenApiSchema mutableEmailSchema)
        {
            mutableEmailSchema.Example = JsonValue.Create("someone@somedomain.com");
        }

        if (mutableSchema.Properties != null
            && mutableSchema.Properties.TryGetValue("password", out IOpenApiSchema? passwordSchema)
            && passwordSchema is OpenApiSchema mutablePasswordSchema)
        {
            mutablePasswordSchema.Example = JsonValue.Create("secret");
        }
    }
}




