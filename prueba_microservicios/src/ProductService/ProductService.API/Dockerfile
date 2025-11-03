FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/ProductService/ProductService.API/ProductService.API.csproj", "src/ProductService/ProductService.API/"]
COPY ["src/ProductService/ProductService.Application/ProductService.Application.csproj", "src/ProductService/ProductService.Application/"]
COPY ["src/ProductService/ProductService.Domain/ProductService.Domain.csproj", "src/ProductService/ProductService.Domain/"]
COPY ["src/ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj", "src/ProductService/ProductService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/ProductService/ProductService.API/ProductService.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/ProductService/ProductService.API"
RUN dotnet build "ProductService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.API.dll"]

