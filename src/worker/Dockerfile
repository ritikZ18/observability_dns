# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files and restore dependencies (better layer caching)
COPY ["src/worker/ObservabilityDns.Worker.csproj", "src/worker/"]
COPY ["src/contracts/ObservabilityDns.Contracts.csproj", "src/contracts/"]
COPY ["src/domain/ObservabilityDns.Domain.csproj", "src/domain/"]
RUN dotnet restore "src/worker/ObservabilityDns.Worker.csproj" --runtime linux-musl-x64

# Copy everything else and build
COPY . .
WORKDIR "/src/src/worker"
RUN dotnet build "ObservabilityDns.Worker.csproj" -c Release -o /app/build --runtime linux-musl-x64 --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "ObservabilityDns.Worker.csproj" -c Release -o /app/publish \
    --runtime linux-musl-x64 \
    --self-contained false \
    /p:UseAppHost=false

# Runtime stage - Alpine-based for smaller image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 -S appgroup && \
    adduser -u 1000 -S appuser -G appgroup

COPY --from=publish /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "ObservabilityDns.Worker.dll"]
