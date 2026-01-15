using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

public class JsonValidator
{
    public List<string> ValidateAndGetMissingLeafLevels(string schemaJson, string dataJson)
    {
        JSchema schema = JSchema.Parse(schemaJson);
        JToken data = JToken.Parse(dataJson);

        List<ValidationError> allErrors = new List<ValidationError>();
        data.Validate(schema, (sender, args) =>
        {
            allErrors.Add(args);
        });

        List<ValidationError> requiredErrors = new List<ValidationError>();
        foreach (var error in allErrors)
        {
            CollectRequiredErrors(error, requiredErrors);
        }

        HashSet<string> missingLeaves = new HashSet<string>();

        foreach (var error in requiredErrors)
        {
            // Parse message: "Required properties are missing from object: prop1, prop2."
            string message = error.Message;
            int colonIndex = message.IndexOf(':');
            if (colonIndex == -1) continue;
            string propsStr = message.Substring(colonIndex + 1).Trim();
            string[] missingProps = propsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(p => p.Trim())
                                            .ToArray();

            string objectPath = error.Path;

            foreach (string prop in missingProps)
            {
                string fullPath = string.IsNullOrEmpty(objectPath) ? prop : objectPath + "." + prop;
                JSchema subSchema = GetSubSchema(schema, fullPath);
                if (subSchema == null) continue;

                List<string> leaves = GetRequiredLeafPaths(subSchema, fullPath);
                foreach (string leaf in leaves)
                {
                    missingLeaves.Add(leaf);
                }
            }
        }

        return missingLeaves.ToList();
    }

    private void CollectRequiredErrors(ValidationError error, List<ValidationError> requiredErrors)
    {
        if (error.ErrorType == ErrorType.Required)
        {
            requiredErrors.Add(error);
        }
        foreach (var child in error.ChildErrors)
        {
            CollectRequiredErrors(child, requiredErrors);
        }
    }

    private JSchema GetSubSchema(JSchema root, string path)
    {
        if (string.IsNullOrEmpty(path)) return root;

        string[] parts = path.Split('.');
        JSchema current = root;

        foreach (string part in parts)
        {
            if (current.Properties == null || !current.Properties.TryGetValue(part, out JSchema next))
            {
                return null;
            }
            current = next;
        }

        return current;
    }

    private List<string> GetRequiredLeafPaths(JSchema schema, string basePath)
    {
        List<string> leaves = new List<string>();

        JSchemaType? type = schema.Type;
        if (type == null) return leaves;

        if (type.Value.HasFlag(JSchemaType.Object))
        {
            IList<string> required = schema.Required ?? new List<string>();
            IDictionary<string, JSchema> properties = schema.Properties ?? new Dictionary<string, JSchema>();

            foreach (string req in required)
            {
                if (properties.TryGetValue(req, out JSchema childSchema))
                {
                    string childPath = string.IsNullOrEmpty(basePath) ? req : basePath + "." + req;
                    leaves.AddRange(GetRequiredLeafPaths(childSchema, childPath));
                }
            }
        }
        else if (type.Value.HasFlag(JSchemaType.Array))
        {
            // Simplified: Skip arrays or handle minimally (extend as needed)
        }
        else // Primitive types (string, number, integer, boolean, null)
        {
            leaves.Add(basePath);
        }

        return leaves;
    }
}
