# Base runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage with SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy required project folders
## API
COPY SensorLayer/Services/SensorService/SensorStore SensorLayer/Services/SensorService/SensorStore/
## Databases
COPY SensorLayer/Databases/DetectionDB/DetectionData SensorLayer/Databases/DetectionDB/DetectionData/
COPY SensorLayer/Databases/DetectionCacheDB/DetectionCacheData SensorLayer/Databases/DetectionCacheDB/DetectionCacheData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/

# Restore dependencies
RUN dotnet restore SensorLayer/Services/SensorService/SensorStore/SensorStore.csproj

# Build the application
RUN dotnet build SensorLayer/Services/SensorService/SensorStore/SensorStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish SensorLayer/Services/SensorService/SensorStore/SensorStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SensorStore.dll"]
