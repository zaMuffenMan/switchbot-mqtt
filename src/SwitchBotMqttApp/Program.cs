using Blazored.Modal;
using FluffySpoon.Ngrok;
using HomeAssistantAddOn.Core;
using Microsoft.AspNetCore.DataProtection;
using HomeAssistantAddOn.Mqtt;
using Microsoft.AspNetCore.HttpOverrides;
using SwitchBotMqttApp.Components;
using Microsoft.Extensions.Configuration;
using SwitchBotMqttApp.Configurations;
using SwitchBotMqttApp.Logics;
using SwitchBotMqttApp.Services;
using System.Net.Http.Headers;
using static HomeAssistantAddOn.Mqtt.SupervisorApi;

namespace SwitchBotMqttApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenAnyIP(8098);//ForWebhook
            serverOptions.ListenAnyIP(8099);//ForWebUI
        });
        builder.Configuration.AddHomeAssistantAddOnConfig();

        var userConfigPath = Path.Combine(HomeAssistantAddOn.Core.Utility.GetBaseDataDirectory(), "user_config.json");
        builder.Configuration.AddUserConfiguration(userConfigPath);

        // Register as singleton
        builder.Services.AddSingleton<UserConfigManager>();

        builder.Services.AddOptions<Configurations.CommonOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                // Bind from appsettings.json first
                configuration.Bind(settings);

                // Override with user config if available
                var userConfig = configuration.GetSection("Common").Get<Configurations.CommonOptions>();
                if (userConfig != null)
                {
                    settings.AutoStartServices = userConfig.AutoStartServices;
                    settings.DeviceStatePersistence = userConfig.DeviceStatePersistence;
                }
            });
        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddBlazoredModal();

        builder.Services.AddOptions<Configurations.CommonOptions>().
            Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.Bind(settings);
            });
        builder.Services.AddOptions<EnforceDeviceTypeOptions>().
            Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("EnforceDeviceTypes").Bind(settings);
            });


        builder.Services.AddSingleton<DeviceConfigurationManager>();
        builder.Services.AddSingleton<DeviceDefinitionsManager>();
        builder.Services.AddSingleton<DeviceStatePersistanceManager>();
        builder.Services.AddSingleton<SwitchBotApiClient>();
        builder.Services.AddSingleton<AppSettingsManager>();
        builder.Services.AddHttpContextAccessor();

#if DEBUG
        builder.Services.AddHttpClient(nameof(SwitchBotApiClient));
#else
        builder.Services.AddHttpClient(nameof(SwitchBotApiClient));
#endif

        builder.Services.AddNgrok(options =>
        {
            var config = builder.Configuration.GetSection("WebhookService").Get<WebhookServiceOptions>()!;
            if (config.UseNgrok)
            {
                options.AuthToken = config.NgrokAuthToken;
#if DEBUG
                options.ShowNgrokWindow = true;
#else
                options.ShowNgrokWindow = false;
#endif
            }
        });
        builder.Services.AddSingleton<MqttCoreService>();
        builder.Services.AddSingleton<PollingService>();
        builder.Services.AddSingleton<WebhookService>();
        builder.Services.AddHostedService<AutomatedHostedService>();

        builder.Services.AddHomeAssistantMqtt();

        builder.Services.AddOptions<SwitchBotOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("SwitchBot").Bind(settings);
            });

        builder.Services.AddOptions<MqttOptions>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("Mqtt").Bind(settings);
            });

        builder.WebHost.UseWebRoot("wwwroot");
        builder.WebHost.UseStaticWebAssets();

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Utility.GetBaseDataDirectory(), "PersistKeys")));

        var app = builder.Build();

        var pathBase = GetPathBase();
        if (pathBase != null)
        {
            app.UsePathBase(pathBase);
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.UseAntiforgery();

        app.Run();
    }

    private static string? GetPathBase()
    {
        if (Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN") == null)
        {
            return null;
        }
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN")! ?? "invalid");

            var response = httpClient.GetFromJsonAsync<SupoervisorResponse<AddonInfo>>("http://supervisor/addons/self/info").Result;
            if (response?.Result != "ok")
            {
                return null;
            }
            return response.Data.IngressEntry;
        }
        catch
        {
            return null;
        }

    }
}