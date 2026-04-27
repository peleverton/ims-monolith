# =============================================================
# US-079: Dockerfile — ims-issues-service
# Extracted microservice for Issues domain
# =============================================================

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/ims-issues-service/ims-issues-service.csproj", "services/ims-issues-service/"]
RUN dotnet restore "services/ims-issues-service/ims-issues-service.csproj"
COPY services/ims-issues-service/ services/ims-issues-service/
WORKDIR "/src/services/ims-issues-service"
RUN dotnet build "ims-issues-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ims-issues-service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ims-issues-service.dll"]
