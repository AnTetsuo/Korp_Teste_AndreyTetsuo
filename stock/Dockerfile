FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Korp.Stock.Api/Korp.Stock.Api.csproj", "src/Korp.Stock.Api/"]
COPY ["src/Korp.Stock.Application/Korp.Stock.Application.csproj", "src/Korp.Stock.Application/"]
COPY ["src/Korp.Stock.Domain/Korp.Stock.Domain.csproj", "src/Korp.Stock.Domain/"]
COPY ["src/Korp.Stock.Infrastructure/Korp.Stock.Infrastructure.csproj", "src/Korp.Stock.Infrastructure/"]
RUN dotnet restore "src/Korp.Stock.Api/Korp.Stock.Api.csproj"

COPY . .
RUN dotnet publish "src/Korp.Stock.Api/Korp.Stock.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "Korp.Stock.Api.dll"]
