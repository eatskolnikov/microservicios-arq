FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/AuthService/AuthService.API/AuthService.API.csproj", "src/AuthService/AuthService.API/"]
COPY ["src/AuthService/AuthService.Application/AuthService.Application.csproj", "src/AuthService/AuthService.Application/"]
COPY ["src/AuthService/AuthService.Domain/AuthService.Domain.csproj", "src/AuthService/AuthService.Domain/"]
COPY ["src/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "src/AuthService/AuthService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/AuthService/AuthService.API/AuthService.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/AuthService/AuthService.API"
RUN dotnet build "AuthService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]

