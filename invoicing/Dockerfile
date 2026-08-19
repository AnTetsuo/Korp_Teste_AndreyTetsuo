FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Korp.Invoicing.Api/Korp.Invoicing.Api.csproj", "src/Korp.Invoicing.Api/"]
COPY ["src/Korp.Invoicing.Application/Korp.Invoicing.Application.csproj", "src/Korp.Invoicing.Application/"]
COPY ["src/Korp.Invoicing.Domain/Korp.Invoicing.Domain.csproj", "src/Korp.Invoicing.Domain/"]
COPY ["src/Korp.Invoicing.Infrastructure/Korp.Invoicing.Infrastructure.csproj", "src/Korp.Invoicing.Infrastructure/"]
RUN dotnet restore "src/Korp.Invoicing.Api/Korp.Invoicing.Api.csproj"

COPY . .
RUN dotnet publish "src/Korp.Invoicing.Api/Korp.Invoicing.Api.csproj" \
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

ENTRYPOINT ["dotnet", "Korp.Invoicing.Api.dll"]
