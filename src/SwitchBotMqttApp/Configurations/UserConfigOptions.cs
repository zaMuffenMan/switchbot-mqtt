namespace SwitchBotMqttApp.Configurations;

public class UserConfigOptions
{
    public MqttConfigOptions Mqtt { get; set; } = new();
    public SwitchBotConfigOptions SwitchBot { get; set; } = new();
    public WebhookConfigOptions WebhookService { get; set; } = new();
}

public class MqttConfigOptions
{
    public bool UseAutoConfig { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 1883;
    public string Id { get; set; } = string.Empty;
    public string Pw { get; set; } = string.Empty;
    public bool Tls { get; set; }
}

public class SwitchBotConfigOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public class WebhookConfigOptions
{
    public bool UseWebhook { get; set; } = true;
    public bool UseNgrok { get; set; } = false;
    public string NgrokAuthToken { get; set; } = string.Empty;
    public string HostUrl { get; set; } = string.Empty;
}