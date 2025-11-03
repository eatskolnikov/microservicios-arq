FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/InventoryService/InventoryService.API/InventoryService.API.csproj", "src/InventoryService/InventoryService.API/"]
COPY ["src/InventoryService/InventoryService.Application/InventoryService.Application.csproj", "src/InventoryService/InventoryService.Application/"]
COPY ["src/InventoryService/InventoryService.Domain/InventoryService.Domain.csproj", "src/InventoryService/InventoryService.Domain/"]
COPY ["src/InventoryService/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj", "src/InventoryService/InventoryService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/InventoryService/InventoryService.API/InventoryService.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/InventoryService/InventoryService.API"
RUN dotnet build "InventoryService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.API.dll"]

