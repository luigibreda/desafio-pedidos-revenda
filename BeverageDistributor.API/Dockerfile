FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["BeverageDistributor.API/BeverageDistributor.API.csproj", "BeverageDistributor.API/"]
COPY ["BeverageDistributor.Application/BeverageDistributor.Application.csproj", "BeverageDistributor.Application/"]
COPY ["BeverageDistributor.Infrastructure/BeverageDistributor.Infrastructure.csproj", "BeverageDistributor.Infrastructure/"]

RUN dotnet restore "BeverageDistributor.API/BeverageDistributor.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/BeverageDistributor.API"
RUN dotnet build "BeverageDistributor.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BeverageDistributor.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "BeverageDistributor.API.dll"]
