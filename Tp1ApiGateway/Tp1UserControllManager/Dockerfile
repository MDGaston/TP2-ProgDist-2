FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tp1UserControllManager/Tp1UserControllManager.csproj", "Tp1UserControllManager/"]
RUN dotnet restore "./Tp1UserControllManager/Tp1UserControllManager.csproj"
COPY . .
WORKDIR "/src/Tp1UserControllManager"
RUN dotnet build "./Tp1UserControllManager.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tp1UserControllManager.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tp1UserControllManager.dll"]