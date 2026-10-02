# Base runtime image (final container)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Install required libraries
RUN apt-get update && apt-get install -y libgssapi-krb5-2

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy source code
## API
COPY TrafficLayer/Services/TrafficLightControllerService/TrafficLightControllerStore TrafficLayer/Services/TrafficLightControllerService/TrafficLightControllerStore/
## Databases
COPY TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData TrafficLayer/Databases/TrafficLightCacheDB/TrafficLightCacheData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/

# Restore dependencies
RUN dotnet restore TrafficLayer/Services/TrafficLightControllerService/TrafficLightControllerStore/TrafficLightControllerStore.csproj

# Build project
RUN dotnet build TrafficLayer/Services/TrafficLightControllerService/TrafficLightControllerStore/TrafficLightControllerStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TrafficLayer/Services/TrafficLightControllerService/TrafficLightControllerStore/TrafficLightControllerStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrafficLightControllerStore.dll"]
