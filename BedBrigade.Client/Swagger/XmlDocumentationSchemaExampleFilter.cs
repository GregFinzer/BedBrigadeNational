using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace BedBrigade.Client.Swagger;

public class XmlDocumentationSchemaExampleFilter : ISchemaFilter
{
    private const string ExampleElementName = "example";
    private const string TypeMemberPrefix = "T:";
    private const string PropertyMemberPrefix = "P:";
    private static readonly Lazy<IReadOnlyDictionary<string, string>> ExampleLookup = new(LoadExamples);

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        Type type = context.Type ?? throw new InvalidOperationException("Schema filter context type cannot be null.");
        ApplyTypeExample(schema, type);
        ApplyPropertyExamples(schema, type);
    }

    private static void ApplyTypeExample(IOpenApiSchema schema, Type type)
    {
        string memberName = TypeMemberPrefix + GetXmlTypeName(type);
        if (schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        if (!ExampleLookup.Value.TryGetValue(memberName, out string? exampleText) || string.IsNullOrWhiteSpace(exampleText))
        {
            return;
        }

        mutableSchema.Example = CreateExampleNode(mutableSchema.Type, exampleText);
    }

    private static void ApplyPropertyExamples(IOpenApiSchema schema, Type type)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
        {
            return;
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string memberName = PropertyMemberPrefix + GetXmlTypeName(type) + "." + property.Name;
            if (!ExampleLookup.Value.TryGetValue(memberName, out string? exampleText) || string.IsNullOrWhiteSpace(exampleText))
            {
                continue;
            }

            foreach (string propertyName in GetSchemaPropertyNames(property))
            {
                if (!schema.Properties.TryGetValue(propertyName, out IOpenApiSchema? propertySchema))
                {
                    continue;
                }

                if (propertySchema is OpenApiSchema mutablePropertySchema)
                {
                    mutablePropertySchema.Example = CreateExampleNode(mutablePropertySchema.Type, exampleText);
                }

                break;
            }
        }
    }

    private static IEnumerable<string> GetSchemaPropertyNames(PropertyInfo property)
    {
        JsonPropertyNameAttribute? jsonNameAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (!string.IsNullOrWhiteSpace(jsonNameAttribute?.Name))
        {
            yield return jsonNameAttribute.Name;
        }

        yield return property.Name;
        yield return char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
    }

    private static JsonNode? CreateExampleNode(JsonSchemaType? schemaType, string exampleText)
    {
        if (schemaType.HasValue
            && schemaType.Value.HasFlag(JsonSchemaType.Integer)
            && long.TryParse(exampleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
        {
            return JsonValue.Create(longValue);
        }

        if (schemaType.HasValue
            && schemaType.Value.HasFlag(JsonSchemaType.Number)
            && decimal.TryParse(exampleText, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            return JsonValue.Create(decimalValue);
        }

        if (schemaType.HasValue
            && schemaType.Value.HasFlag(JsonSchemaType.Boolean)
            && bool.TryParse(exampleText, out bool boolValue))
        {
            return JsonValue.Create(boolValue);
        }

        if (schemaType.HasValue
            && (schemaType.Value.HasFlag(JsonSchemaType.Array) || schemaType.Value.HasFlag(JsonSchemaType.Object))
            && TryParseJson(exampleText, out JsonNode? jsonNode))
        {
            return jsonNode;
        }

        return JsonValue.Create(exampleText);
    }

    private static bool TryParseJson(string input, out JsonNode? node)
    {
        try
        {
            node = JsonNode.Parse(input);
            return node is not null;
        }
        catch
        {
            node = null;
            return false;
        }
    }

    private static IReadOnlyDictionary<string, string> LoadExamples()
    {
        Dictionary<string, string> examples = new(StringComparer.Ordinal);
        foreach (string xmlPath in Directory.EnumerateFiles(AppContext.BaseDirectory, "BedBrigade*.xml", SearchOption.TopDirectoryOnly))
        {
            LoadExamplesFromFile(examples, xmlPath);
        }

        return examples;
    }

    private static void LoadExamplesFromFile(IDictionary<string, string> examples, string xmlPath)
    {
        try
        {
            XDocument document = XDocument.Load(xmlPath);
            IEnumerable<XElement> members = document.Descendants("member");
            foreach (XElement member in members)
            {
                string? memberName = member.Attribute("name")?.Value;
                string? exampleValue = member.Element(ExampleElementName)?.Value.Trim();
                if (!string.IsNullOrWhiteSpace(memberName)
                    && !string.IsNullOrWhiteSpace(exampleValue)
                    && !examples.ContainsKey(memberName))
                {
                    examples.Add(memberName, exampleValue);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Unable to read XML documentation examples from {XmlPath}", xmlPath);
        }
    }

    private static string GetXmlTypeName(Type type)
    {
        string fullName = type.FullName ?? type.Name;
        return fullName.Replace('+', '.');
    }
}

