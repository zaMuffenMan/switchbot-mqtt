using HomeAssistantAddOn.Core;
using SwitchBotMqttApp.Configurations;
using System.Text.Json;

namespace SwitchBotMqttApp.Services;

public class UserConfigManager
{
    private static readonly string UserConfigFilePath = Path.Combine(Utility.GetBaseDataDirectory(), "user_config.json");
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<UserConfigManager> _logger;
    private readonly IConfiguration _configuration;

    public UserConfigManager(ILogger<UserConfigManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserConfigOptions> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(UserConfigFilePath))
        {
            var json = await File.ReadAllTextAsync(UserConfigFilePath, cancellationToken);
            var config = JsonSerializer.Deserialize<UserConfigOptions>(json, JsonSerializerOptions);
            _logger.LogInformation("User configuration loaded from {path}", UserConfigFilePath);
            return config ?? new UserConfigOptions();
        }
        
        _logger.LogInformation("User configuration file not found, using defaults");
        return new UserConfigOptions();
    }

    public async Task SaveConfigAsync(UserConfigOptions config, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(UserConfigFilePath)!);
        
        // Backup existing config
        if (File.Exists(UserConfigFilePath))
        {
            var backup = Path.Combine(
                Path.GetDirectoryName(UserConfigFilePath)!, 
                $"user_config_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.json"
            );
            File.Copy(UserConfigFilePath, backup);
            _logger.LogInformation("Backup created: {backup}", backup);
        }

        var json = JsonSerializer.Serialize(config, JsonSerializerOptions);
        await File.WriteAllTextAsync(UserConfigFilePath, json, cancellationToken);
        _logger.LogInformation("User configuration saved to {path}", UserConfigFilePath);

        // Small delay to ensure file system has flushed
        await Task.Delay(100, cancellationToken);

        // Reload configuration to trigger IOptionsMonitor change notifications
        ReloadConfiguration();
        
        _logger.LogInformation("Configuration reload completed");
    }

    private void ReloadConfiguration()
    {
        var root = _configuration as IConfigurationRoot;
        if (root != null)
        {
            var provider = root.Providers
                .OfType<UserConfigurationProvider>()
                .FirstOrDefault();

            if (provider != null)
            {
                provider.Reload();
                _logger.LogInformation("UserConfigurationProvider reloaded - change token triggered");
            }
            else
            {
                _logger.LogWarning("UserConfigurationProvider not found in configuration");
            }
        }
        else
        {
            _logger.LogWarning("Configuration is not IConfigurationRoot");
        }
    }
}