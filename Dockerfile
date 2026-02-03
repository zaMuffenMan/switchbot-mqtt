#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8099
EXPOSE 8098

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/SwitchBotMqttApp/SwitchBotMqttApp.csproj", "src/SwitchBotMqttApp/"]
COPY ["src/HomeAssistant/HomeAssistantAddOn.Core/HomeAssistantAddOn.Core.csproj", "src/HomeAssistant/HomeAssistantAddOn.Core/"]
COPY ["src/HomeAssistant/HomeAssistantAddOn.Mqtt/HomeAssistantAddOn.Mqtt.csproj", "src/HomeAssistant/HomeAssistantAddOn.Mqtt/"]
RUN dotnet restore "src/SwitchBotMqttApp/SwitchBotMqttApp.csproj"
COPY . .
WORKDIR "/src/src/SwitchBotMqttApp"
RUN dotnet build "SwitchBotMqttApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SwitchBotMqttApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SwitchBotMqttApp.dll"]