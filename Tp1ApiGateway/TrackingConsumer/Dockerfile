FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8002

# Copia y restaura dependencias
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TrackingConsumer/TrackingConsumer.csproj", "TrackingConsumer/"]
RUN dotnet restore "./TrackingConsumer/TrackingConsumer.csproj"
COPY . .
WORKDIR "/src/TrackingConsumer"
RUN dotnet build "./TrackingConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publica la aplicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TrackingConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Configura la imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Asegura que appsettings.json esté presente en la imagen final
COPY ./TrackingConsumer/appsettings.json /app/appsettings.json

# Configura las variables de entorno para Serilog
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=8002
ENV SERILOG__WRITEAT=File
ENV SERILOG__FILE__PATH=/app/trackingLog/log-.txt
ENV SERILOG__FILE__ROLLINGINTERVAL=Day


# Entry point que incluye un sleep antes de iniciar la aplicación
ENTRYPOINT ["sh", "-c", "sleep 30 && dotnet TrackingConsumer.dll"]
