FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
# Copy shared contracts first
COPY shared/SharedContracts/SharedContracts.csproj shared/SharedContracts/

# Copy core projects
COPY core/WebService/WebService.csproj core/WebService/
COPY core/CameraManager/CameraManager.csproj core/CameraManager/
COPY core/Persistence/Persistence.csproj core/Persistence/
COPY core/CameraControllerConnector/CameraControllerConnector.csproj core/CameraControllerConnector/

# Restore dependencies
RUN dotnet restore core/WebService/WebService.csproj

# Copy source code
COPY shared/SharedContracts/ shared/SharedContracts/
COPY core/CameraControllerConnector/ core/CameraControllerConnector/
COPY core/Persistence/ core/Persistence/
COPY core/CameraManager/ core/CameraManager/
COPY core/WebService/ core/WebService/

# Build the application
WORKDIR /src/core/WebService
RUN dotnet build WebService.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish WebService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000 5001

# Copy the published application
COPY --from=publish /app/publish .

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000,https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "WebService.dll"]
