# Base runtime image (final container)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Install dependencies required for Npgsql (Kerberos libraries)
RUN apt-get update && apt-get install -y libgssapi-krb5-2

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy source code
## API
COPY TrafficLayer/Services/TrafficLightCoordinatorService/TrafficLightCoordinatorStore TrafficLayer/Services/TrafficLightCoordinatorService/TrafficLightCoordinatorStore/
## Databases
COPY TrafficLayer/Databases/TrafficLightDB/TrafficLightData TrafficLayer/Databases/TrafficLightDB/TrafficLightData/
COPY TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/


# Restore NuGet packages
RUN dotnet restore TrafficLayer/Services/TrafficLightCoordinatorService/TrafficLightCoordinatorStore/TrafficLightCoordinatorStore.csproj

# Build project
RUN dotnet build TrafficLayer/Services/TrafficLightCoordinatorService/TrafficLightCoordinatorStore/TrafficLightCoordinatorStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TrafficLayer/Services/TrafficLightCoordinatorService/TrafficLightCoordinatorStore/TrafficLightCoordinatorStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrafficLightCoordinatorStore.dll"]
