FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
# Copy shared contracts first
COPY shared/SharedContracts/SharedContracts.csproj shared/SharedContracts/

# Copy camera-controller projects
COPY camera-controller/Contracts/Contracts.csproj camera-controller/Contracts/
COPY camera-controller/RtspCamera/RtspCamera.csproj camera-controller/RtspCamera/
COPY camera-controller/WebService/WebService.csproj camera-controller/WebService/

# Restore dependencies
RUN dotnet restore camera-controller/WebService/WebService.csproj

# Copy source code
COPY shared/SharedContracts/ shared/SharedContracts/
COPY camera-controller/Contracts/ camera-controller/Contracts/
COPY camera-controller/RtspCamera/ camera-controller/RtspCamera/
COPY camera-controller/WebService/ camera-controller/WebService/

# Build the application
WORKDIR /src/camera-controller/WebService
RUN dotnet build WebService.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish WebService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000

# Install FFmpeg and other dependencies
RUN apt-get update && \
    apt-get install -y \
    ffmpeg \
    iputils-ping \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "WebService.dll"]