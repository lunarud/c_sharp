using System.Text.Json;
using System.Text.Json.Nodes;

public class SchemaValidationResult
{
    public bool IsValid => !MissingPaths.Any() && !Errors.Any();
    public List<string> MissingPaths { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class JsonSchemaValidator
{
    public SchemaValidationResult Validate(string json, string schema)
    {
        var result = new SchemaValidationResult();
        
        try
        {
            var dataNode = JsonNode.Parse(json);
            var schemaNode = JsonNode.Parse(schema);
            
            ValidateNode(dataNode, schemaNode, "", result);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Parse error: {ex.Message}");
        }
        
        return result;
    }

    private void ValidateNode(JsonNode? data, JsonNode? schema, string path, SchemaValidationResult result)
    {
        if (schema == null) return;

        var schemaObj = schema.AsObject();
        var type = schemaObj["type"]?.GetValue<string>();

        switch (type)
        {
            case "object":
                ValidateObject(data, schemaObj, path, result);
                break;
            case "array":
                ValidateArray(data, schemaObj, path, result);
                break;
            default:
                ValidateLeaf(data, path, result);
                break;
        }
    }

    private void ValidateObject(JsonNode? data, JsonObject schema, string path, SchemaValidationResult result)
    {
        var properties = schema["properties"]?.AsObject();
        var required = schema["required"]?.AsArray()
            .Select(n => n?.GetValue<string>())
            .Where(s => s != null)
            .ToHashSet() ?? new HashSet<string?>();

        if (properties == null) return;

        var dataObj = data?.AsObject();

        foreach (var prop in properties)
        {
            var propPath = string.IsNullOrEmpty(path) ? prop.Key : $"{path}.{prop.Key}";
            var propData = dataObj?[prop.Key];

            if (propData == null && required.Contains(prop.Key))
            {
                CollectMissingLeaves(prop.Value, propPath, result);
            }
            else if (propData != null)
            {
                ValidateNode(propData, prop.Value, propPath, result);
            }
        }
    }

    private void ValidateArray(JsonNode? data, JsonObject schema, string path, SchemaValidationResult result)
    {
        var items = schema["items"];
        if (items == null || data == null) return;

        var dataArray = data.AsArray();
        for (int i = 0; i < dataArray.Count; i++)
        {
            ValidateNode(dataArray[i], items, $"{path}[{i}]", result);
        }
    }

    private void ValidateLeaf(JsonNode? data, string path, SchemaValidationResult result)
    {
        if (data == null || IsNullOrEmpty(data))
        {
            result.MissingPaths.Add(path);
        }
    }

    private void CollectMissingLeaves(JsonNode? schema, string path, SchemaValidationResult result)
    {
        if (schema == null) return;

        var schemaObj = schema.AsObject();
        var type = schemaObj["type"]?.GetValue<string>();

        if (type == "object")
        {
            var properties = schemaObj["properties"]?.AsObject();
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    CollectMissingLeaves(prop.Value, $"{path}.{prop.Key}", result);
                }
            }
        }
        else if (type == "array")
        {
            // Array itself is missing - report the path
            result.MissingPaths.Add(path);
        }
        else
        {
            // Leaf node
            result.MissingPaths.Add(path);
        }
    }

    private bool IsNullOrEmpty(JsonNode node)
    {
        return node.GetValueKind() == JsonValueKind.Null ||
               (node.GetValueKind() == JsonValueKind.String && string.IsNullOrEmpty(node.GetValue<string>()));
    }
}
