# Base runtime image (final container)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy source code
## API
COPY TrafficLayer/Services/IntersectionControllerService/IntersectionControllerStore TrafficLayer/Services/IntersectionControllerService/IntersectionControllerStore/
## Databases
COPY TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData/
COPY SensorLayer/Databases/DetectionCacheDB/DetectionCacheData SensorLayer/Databases/DetectionCacheDB/DetectionCacheData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/

# Restore dependencies
RUN dotnet restore TrafficLayer/Services/IntersectionControllerService/IntersectionControllerStore/IntersectionControllerStore.csproj

# Build project
RUN dotnet build TrafficLayer/Services/IntersectionControllerService/IntersectionControllerStore/IntersectionControllerStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TrafficLayer/Services/IntersectionControllerService/IntersectionControllerStore/IntersectionControllerStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IntersectionControllerStore.dll"]
