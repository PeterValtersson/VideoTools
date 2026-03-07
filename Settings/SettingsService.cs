using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using VideoTools.Settings;

public class SettingsService
{
    private readonly IConfiguration _configRoot;

    public SettingsService(IConfiguration config)
    {
        _configRoot = config;
    }

    public void Save<TValue>(string field, TValue value)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Settings/appsettings.json");

        var json = File.ReadAllText(path);
        var jsonNode = JsonNode.Parse(json);

        if (jsonNode == null)
            return;

        jsonNode[field] = JsonSerializer.SerializeToNode(value);

        File.WriteAllText(path,
            jsonNode.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }
}