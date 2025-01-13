using System;
using System.Collections.Generic;
using System.Text.Json; // Add System.Text.Json for JSON serialization

class Program
{
    static void Main()
    {
        // Sample NVFIX data
        string nvfixData = "key1=value1;key2=value2;key3=value3";

        // Parse NVFIX data into a dictionary
        Dictionary<string, string> data = ParseNVFIX(nvfixData);

        // Convert dictionary to JSON
        string json = JsonSerializer.Serialize(data);

        // Output the JSON
        Console.WriteLine(json);
    }

    static Dictionary<string, string> ParseNVFIX(string nvfixData)
    {
        var result = new Dictionary<string, string>();

        // Split key-value pairs by ';'
        string[] pairs = nvfixData.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (string pair in pairs)
        {
            // Split key and value by '='
            string[] keyValue = pair.Split('=', StringSplitOptions.RemoveEmptyEntries);

            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();
                result[key] = value;
            }
        }

        return result;
    }
}
