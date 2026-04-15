# =============================================================
# US-025: Multi-stage Dockerfile — IMS Modular Monolith
# =============================================================
# Stages:
#   1. restore  — nuget restore (cache layer)
#   2. build    — dotnet build
#   3. publish  — dotnet publish (Release, trimmed)
#   4. runtime  — final lightweight image (aspnet runtime only)

# ── 1. Restore ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restore
WORKDIR /src

# Copy only csproj first — leverage Docker layer cache for nuget restore
COPY src/*.csproj ./
RUN dotnet restore --locked-mode || dotnet restore

# ── 2. Build ─────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src

# Copy full source
COPY src/ ./

RUN dotnet build -c Release --no-restore -o /build

# ── 3. Publish ───────────────────────────────────────────────
FROM build AS publish

RUN dotnet publish -c Release --no-build -o /publish \
    /p:UseAppHost=false

# ── 4. Runtime ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Security: run as non-root
RUN addgroup -S ims && adduser -S ims -G ims
WORKDIR /app

# Copy published output
COPY --from=publish /publish .

# Healthcheck (matches /health/live endpoint)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1

# Switch to non-root user
USER ims

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ims-monolith.dll"]
