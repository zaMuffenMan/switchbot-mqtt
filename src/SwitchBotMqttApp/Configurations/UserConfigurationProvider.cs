using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace SwitchBotMqttApp.Configurations;

public class UserConfigurationProvider : ConfigurationProvider
{
    private readonly string _filePath;
    private ConfigurationReloadToken _reloadToken = new();

    public UserConfigurationProvider(string filePath)
    {
        _filePath = filePath;
    }

    public override void Load()
    {
        Data.Clear();
        
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var config = JsonSerializer.Deserialize<UserConfigOptions>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            if (config != null)
            {
                Data["Mqtt:UseAutoConfig"] = config.Mqtt.UseAutoConfig.ToString().ToLowerInvariant();
                Data["Mqtt:Host"] = config.Mqtt.Host ?? string.Empty;
                Data["Mqtt:Port"] = config.Mqtt.Port.ToString();
                Data["Mqtt:Id"] = config.Mqtt.Id ?? string.Empty;
                Data["Mqtt:Pw"] = config.Mqtt.Pw ?? string.Empty;
                Data["Mqtt:Tls"] = config.Mqtt.Tls.ToString().ToLowerInvariant();
                Data["SwitchBot:ApiKey"] = config.SwitchBot.ApiKey ?? string.Empty;
                Data["SwitchBot:ApiSecret"] = config.SwitchBot.ApiSecret ?? string.Empty;
                Data["WebhookService:UseWebhook"] = config.WebhookService.UseWebhook.ToString().ToLowerInvariant();
                Data["WebhookService:UseNgrok"] = config.WebhookService.UseNgrok.ToString().ToLowerInvariant();
                Data["WebhookService:NgrokAuthToken"] = config.WebhookService.NgrokAuthToken ?? string.Empty;
                Data["WebhookService:HostUrl"] = config.WebhookService.HostUrl ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user configuration: {ex.Message}");
        }
    }

    public void Reload()
    {
        var previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
        Load();
        previousToken.OnReload();
    }

    public IChangeToken GetReloadToken()
    {
        return _reloadToken;
    }
}

public class UserConfigurationSource : IConfigurationSource
{
    private readonly string _filePath;

    public UserConfigurationSource(string filePath)
    {
        _filePath = filePath;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new UserConfigurationProvider(_filePath);
    }
}

public static class UserConfigurationExtensions
{
    public static IConfigurationBuilder AddUserConfiguration(this IConfigurationBuilder builder, string filePath)
    {
        return builder.Add(new UserConfigurationSource(filePath));
    }
}