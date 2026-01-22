# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ResilientGate.Gateway/ResilientGate.Gateway.csproj", "ResilientGate.Gateway/"]
RUN dotnet restore "ResilientGate.Gateway/ResilientGate.Gateway.csproj"

# Copy everything else and build
COPY ResilientGate.Gateway/ ResilientGate.Gateway/
WORKDIR "/src/ResilientGate.Gateway"
RUN dotnet build "ResilientGate.Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ResilientGate.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResilientGate.Gateway.dll"]
