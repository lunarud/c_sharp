
// Add options for customization
public class SchemaGeneratorOptions
{
    public bool InferFormats { get; set; } = true;
    public bool MarkAllRequired { get; set; } = true;
    public bool AddExamples { get; set; } = false;
    public bool AddDescriptions { get; set; } = false;
}


{
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "name": { "type": "string" },
    "email": { "type": "string", "format": "email" },
    "website": { "type": "string", "format": "uri" },
    "rating": { "type": "number" },
    "isActive": { "type": "boolean" },
    "createdAt": { "type": "string", "format": "date-time" },
    "address": {
      "type": "object",
      "properties": {
        "street": { "type": "string" },
        "city": { "type": "string" },
        "zip": { "type": "string" }
      },
      "required": ["street", "city", "zip"]
    },
    "inspections": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "date": { "type": "string", "format": "date-time" },
          "score": { "type": "integer" },
          "passed": { "type": "boolean" }
        },
        "required": ["date", "score", "passed"]
      }
    },
    "tags": {
      "type": "array",
      "items": { "type": "string" }
    }
  },
  "required": ["id", "name", "email", "website", "rating", "isActive", "createdAt", "address", "inspections", "tags"]
}
