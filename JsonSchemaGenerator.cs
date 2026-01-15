using System.Text.Json;
using System.Text.Json.Nodes;

public class JsonSchemaGenerator
{
    public JsonObject Generate(string json)
    {
        var node = JsonNode.Parse(json);
        return GenerateSchema(node);
    }

    private JsonObject GenerateSchema(JsonNode? node)
    {
        if (node == null)
            return new JsonObject { ["type"] = "null" };

        return node.GetValueKind() switch
        {
            JsonValueKind.Object => GenerateObjectSchema(node.AsObject()),
            JsonValueKind.Array => GenerateArraySchema(node.AsArray()),
            JsonValueKind.String => GenerateStringSchema(node.GetValue<string>()),
            JsonValueKind.Number => GenerateNumberSchema(node),
            JsonValueKind.True or JsonValueKind.False => new JsonObject { ["type"] = "boolean" },
            JsonValueKind.Null => new JsonObject { ["type"] = "null" },
            _ => new JsonObject()
        };
    }

    private JsonObject GenerateObjectSchema(JsonObject obj)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var prop in obj)
        {
            properties[prop.Key] = GenerateSchema(prop.Value);
            required.Add(prop.Key);
        }

        if (properties.Count > 0)
        {
            schema["properties"] = properties;
            schema["required"] = required;
        }

        return schema;
    }

    private JsonObject GenerateArraySchema(JsonArray array)
    {
        var schema = new JsonObject
        {
            ["type"] = "array"
        };

        if (array.Count > 0)
        {
            // Merge schemas from all items for heterogeneous arrays
            schema["items"] = MergeSchemas(array.Select(GenerateSchema).ToList());
        }

        return schema;
    }

    private JsonObject GenerateStringSchema(string value)
    {
        var schema = new JsonObject { ["type"] = "string" };

        // Detect common formats
        if (DateTime.TryParse(value, out _))
            schema["format"] = "date-time";
        else if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && 
                 (uri.Scheme == "http" || uri.Scheme == "https"))
            schema["format"] = "uri";
        else if (IsEmail(value))
            schema["format"] = "email";
        else if (Guid.TryParse(value, out _))
            schema["format"] = "uuid";

        return schema;
    }

    private JsonObject GenerateNumberSchema(JsonNode node)
    {
        // Check if integer or decimal
        var raw = node.ToJsonString();
        return new JsonObject
        {
            ["type"] = raw.Contains('.') ? "number" : "integer"
        };
    }

    private JsonObject MergeSchemas(List<JsonObject> schemas)
    {
        if (schemas.Count == 0)
            return new JsonObject();

        if (schemas.Count == 1)
            return schemas[0];

        var types = schemas
            .Select(s => s["type"]?.GetValue<string>())
            .Distinct()
            .ToList();

        // All same type - merge properties if objects
        if (types.Count == 1 && types[0] == "object")
        {
            return MergeObjectSchemas(schemas);
        }

        // Mixed types - use oneOf
        if (types.Count > 1)
        {
            return new JsonObject
            {
                ["oneOf"] = new JsonArray(schemas.Select(s => JsonNode.Parse(s.ToJsonString())).ToArray())
            };
        }

        return schemas[0];
    }

    private JsonObject MergeObjectSchemas(List<JsonObject> schemas)
    {
        var merged = new JsonObject { ["type"] = "object" };
        var allProperties = new Dictionary<string, List<JsonObject>>();
        var requiredSets = new List<HashSet<string>>();

        foreach (var schema in schemas)
        {
            var props = schema["properties"]?.AsObject();
            if (props != null)
            {
                foreach (var prop in props)
                {
                    if (!allProperties.ContainsKey(prop.Key))
                        allProperties[prop.Key] = new();
                    
                    allProperties[prop.Key].Add(prop.Value?.AsObject() ?? new JsonObject());
                }
            }

            var req = schema["required"]?.AsArray()
                .Select(n => n?.GetValue<string>() ?? "")
                .ToHashSet() ?? new HashSet<string>();
            requiredSets.Add(req);
        }

        var mergedProps = new JsonObject();
        foreach (var kvp in allProperties)
        {
            mergedProps[kvp.Key] = MergeSchemas(kvp.Value);
        }

        // Only required if present in ALL schemas
        var commonRequired = requiredSets.Count > 0
            ? requiredSets.Aggregate((a, b) => a.Intersect(b).ToHashSet())
            : new HashSet<string>();

        if (mergedProps.Count > 0)
            merged["properties"] = mergedProps;

        if (commonRequired.Count > 0)
            merged["required"] = new JsonArray(commonRequired.Select(s => JsonNode.Parse($"\"{s}\"")).ToArray());

        return merged;
    }

    private bool IsEmail(string value)
    {
        var idx = value.IndexOf('@');
        return idx > 0 && idx < value.Length - 1 && value.IndexOf('.', idx) > idx;
    }
}
