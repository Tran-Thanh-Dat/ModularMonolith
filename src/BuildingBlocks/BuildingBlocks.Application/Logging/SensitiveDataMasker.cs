using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Application.Logging;

public sealed class SensitiveDataMasker
{
    private const string MaskValue = "******";

    private readonly HashSet<string> _sensitiveFields;
    private readonly string[] _sensitivePatterns;

    public SensitiveDataMasker(IOptions<SensitiveDataOptions> options)
    {
        var fields = options.Value.SensitiveFields ?? [];
        _sensitiveFields = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
        _sensitivePatterns = options.Value.SensitivePatterns ?? [];
    }

    public object? MaskObject(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            var json = JsonSerializer.Serialize(value);
            var maskedJson = MaskJson(json);
            return maskedJson;
        }
        catch
        {
            return MaskValue;
        }
    }

    public string MaskJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            MaskNode(node);
            return node.ToJsonString();
        }
        catch
        {
            return json;
        }
    }

    private void MaskNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Key is not null && IsSensitiveField(property.Key))
                    {
                        jsonObject[property.Key] = MaskValue;
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        MaskNode(property.Value);
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    if (item is not null)
                    {
                        MaskNode(item);
                    }
                }

                break;
        }
    }

    private bool IsSensitiveField(string fieldName)
    {
        if (_sensitiveFields.Contains(fieldName))
        {
            return true;
        }

        return _sensitivePatterns.Any(pattern =>
            fieldName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
