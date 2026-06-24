using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace BedBrigade.Client.Swagger;

public class EnumSchemaFilter : ISchemaFilter
{
    private const string SummaryElementName = "summary";
    private const string TypeMemberPrefix = "T:";
    private const string FieldMemberPrefix = "F:";
    private static readonly Lazy<IReadOnlyDictionary<string, string>> SummaryLookup = new(LoadSummaries);

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        Type? enumType = ResolveEnumType(context.Type);
        if (enumType is null || schema is not OpenApiSchema mutableSchema)
        {
            return;
        }

        mutableSchema.Description = BuildEnumDescription(enumType);
    }

    private static string BuildEnumDescription(Type enumType)
    {
        StringBuilder description = new();
        string enumMemberName = TypeMemberPrefix + GetXmlTypeName(enumType);
        if (SummaryLookup.Value.TryGetValue(enumMemberName, out string? enumSummary) && !string.IsNullOrWhiteSpace(enumSummary))
        {
            description.AppendLine(enumSummary);
            description.AppendLine();
        }

        description.AppendLine("Values:");
        foreach (object enumValue in Enum.GetValues(enumType))
        {
            string enumValueName = Enum.GetName(enumType, enumValue) ?? enumValue.ToString() ?? string.Empty;
            string enumNumericValue = Convert.ToUInt64(enumValue, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            description.Append("* `")
                .Append(enumNumericValue)
                .Append("` - Type: `")
                .Append(enumValueName)
                .Append('`');

            string fieldMemberName = $"{FieldMemberPrefix}{GetXmlTypeName(enumType)}.{enumValueName}";
            if (SummaryLookup.Value.TryGetValue(fieldMemberName, out string? fieldSummary) && !string.IsNullOrWhiteSpace(fieldSummary))
            {
                description.Append(": ").Append(fieldSummary);
            }

            description.AppendLine();
        }

        return description.ToString().TrimEnd();
    }

    private static Type? ResolveEnumType(Type type)
    {
        if (type.IsEnum)
        {
            return type;
        }

        Type? underlyingNullableType = Nullable.GetUnderlyingType(type);
        if (underlyingNullableType?.IsEnum == true)
        {
            return underlyingNullableType;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> LoadSummaries()
    {
        Dictionary<string, string> summaries = new(StringComparer.Ordinal);
        foreach (string xmlPath in Directory.EnumerateFiles(AppContext.BaseDirectory, "BedBrigade*.xml", SearchOption.TopDirectoryOnly))
        {
            LoadSummariesFromFile(summaries, xmlPath);
        }

        return summaries;
    }

    private static void LoadSummariesFromFile(IDictionary<string, string> summaries, string xmlPath)
    {
        try
        {
            XDocument document = XDocument.Load(xmlPath);
            IEnumerable<XElement> members = document.Descendants("member");
            foreach (XElement member in members)
            {
                string? memberName = member.Attribute("name")?.Value;
                string? summary = member.Element(SummaryElementName)?.Value;
                if (!string.IsNullOrWhiteSpace(memberName)
                    && !string.IsNullOrWhiteSpace(summary)
                    && !summaries.ContainsKey(memberName))
                {
                    summaries.Add(memberName, NormalizeSummary(summary));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Unable to read XML documentation summaries from {XmlPath}", xmlPath);
        }
    }

    private static string NormalizeSummary(string summary)
    {
        return string.Join(" ", summary
            .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim()));
    }

    private static string GetXmlTypeName(Type type)
    {
        string fullName = type.FullName ?? type.Name;
        return fullName.Replace('+', '.');
    }
}
