# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ResilientGate.FlakyService/ResilientGate.FlakyService.csproj", "ResilientGate.FlakyService/"]
RUN dotnet restore "ResilientGate.FlakyService/ResilientGate.FlakyService.csproj"

# Copy everything else and build
COPY ResilientGate.FlakyService/ ResilientGate.FlakyService/
WORKDIR "/src/ResilientGate.FlakyService"
RUN dotnet build "ResilientGate.FlakyService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ResilientGate.FlakyService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResilientGate.FlakyService.dll"]
