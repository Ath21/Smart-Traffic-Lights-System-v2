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
COPY UserLayer/Services/UserService/UserStore UserLayer/Services/UserService/UserStore/
## Databases
COPY UserLayer/Databases/UserDB/UserData UserLayer/Databases/UserDB/UserData/
## Messages
COPY MessageLayer/Messages MessageLayer/Messages/

# Restore dependencies
RUN dotnet restore UserLayer/Services/UserService/UserStore/UserStore.csproj

# Build the application
RUN dotnet build UserLayer/Services/UserService/UserStore/UserStore.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish UserLayer/Services/UserService/UserStore/UserStore.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserStore.dll"]
