using HomeAssistantAddOn.Mqtt;
using Microsoft.Extensions.Options;
using SwitchBotMqttApp.Configurations;
using System.Text.Json;

namespace SwitchBotMqttApp.Services;

public class AppSettingsManager
{
    private readonly ILogger<AppSettingsManager> _logger;
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettingsManager(ILogger<AppSettingsManager> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    private string AppSettingsPath => Path.Combine(_environment.ContentRootPath, "appsettings.json");

    public async Task<(MqttOptions Mqtt, SwitchBotOptions SwitchBot)> GetSettingsAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(AppSettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var mqtt = new MqttOptions();
            var switchBot = new SwitchBotOptions();

            if (root.TryGetProperty("Mqtt", out var mqttElement))
            {
                mqtt = JsonSerializer.Deserialize<MqttOptions>(mqttElement.GetRawText(), JsonSerializerOptions) ?? new MqttOptions();
            }

            if (root.TryGetProperty("SwitchBot", out var switchBotElement))
            {
                switchBot = JsonSerializer.Deserialize<SwitchBotOptions>(switchBotElement.GetRawText(), JsonSerializerOptions) ?? new SwitchBotOptions();
            }

            return (mqtt, switchBot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading appsettings.json");
            return (new MqttOptions(), new SwitchBotOptions());
        }
    }

    public async Task<bool> SaveSettingsAsync(MqttOptions mqtt, SwitchBotOptions switchBot)
    {
        try
        {
            var json = await File.ReadAllTextAsync(AppSettingsPath);
            using var doc = JsonDocument.Parse(json);
            
            // Create a mutable dictionary from the existing settings
            var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonSerializerOptions) 
                ?? new Dictionary<string, JsonElement>();

            // Update Mqtt section
            var mqttJson = JsonSerializer.SerializeToElement(mqtt, JsonSerializerOptions);
            settings["Mqtt"] = mqttJson;

            // Update SwitchBot section
            var switchBotJson = JsonSerializer.SerializeToElement(switchBot, JsonSerializerOptions);
            settings["SwitchBot"] = switchBotJson;

            // Write back to file
            var updatedJson = JsonSerializer.Serialize(settings, JsonSerializerOptions);
            await File.WriteAllTextAsync(AppSettingsPath, updatedJson);

            _logger.LogInformation("Settings saved successfully to appsettings.json");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings to appsettings.json");
            return false;
        }
    }
}