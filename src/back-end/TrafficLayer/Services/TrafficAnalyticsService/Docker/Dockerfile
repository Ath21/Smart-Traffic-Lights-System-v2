# Base runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy required project folders
## API
COPY TrafficLayer/Services/TrafficAnalyticsService/TrafficAnalyticsStore TrafficLayer/Services/TrafficAnalyticsService/TrafficAnalyticsStore/
## Databases
COPY TrafficLayer/Databases/TrafficAnalyticsDB/TrafficAnalyticsData TrafficLayer/Databases/TrafficAnalyticsDB/TrafficAnalyticsData/
COPY SensorLayer/Databases/DetectionDB/DetectionData SensorLayer/Databases/DetectionDB/DetectionData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/

# Restore dependencies
RUN dotnet restore TrafficLayer/Services/TrafficAnalyticsService/TrafficAnalyticsStore/TrafficAnalyticsStore.csproj

# Build the project
RUN dotnet build TrafficLayer/Services/TrafficAnalyticsService/TrafficAnalyticsStore/TrafficAnalyticsStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish the project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TrafficLayer/Services/TrafficAnalyticsService/TrafficAnalyticsStore/TrafficAnalyticsStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrafficAnalyticsStore.dll"]
