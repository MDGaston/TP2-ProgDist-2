FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tp1ApiGateway/Tp1ApiGateway.csproj", "Tp1ApiGateway/"]
RUN dotnet restore "./Tp1ApiGateway/Tp1ApiGateway.csproj"
COPY . .
WORKDIR "/src/Tp1ApiGateway"
RUN dotnet build "./Tp1ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tp1ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tp1ApiGateway.dll"]