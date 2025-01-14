 using System;
using System.Collections.Generic;
using System.Text.Json;

class Program
{
    static void Main()
    {
        // Sample JSON input
        string jsonInput = @"
        {
            ""key1"": ""value1"",
            ""key2"": ""value2"",
            ""key3"": ""value3""
        }";

        // Deserialize JSON into a dictionary
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonInput);

        // Convert dictionary to NVFIX format
        string nvfix = ConvertToNVFIX(data);

        // Output NVFIX format
        Console.WriteLine("NVFIX Output:");
        Console.WriteLine(nvfix);
    }

    static string ConvertToNVFIX(Dictionary<string, string> data)
    {
        // Convert key-value pairs into NVFIX format (key=value;key=value;)
        return string.Join(";", data.Select(kvp => $"{kvp.Key}={kvp.Value}")) + ";";
    }
}
