# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching on restore.
COPY LzDetailingStore.slnx ./
COPY src/LzDetailingStore.Api/LzDetailingStore.Api.csproj src/LzDetailingStore.Api/
COPY src/LzDetailingStore.Application/LzDetailingStore.Application.csproj src/LzDetailingStore.Application/
COPY src/LzDetailingStore.Domain/LzDetailingStore.Domain.csproj src/LzDetailingStore.Domain/
COPY src/LzDetailingStore.Infrastructure/LzDetailingStore.Infrastructure.csproj src/LzDetailingStore.Infrastructure/
COPY src/LzDetailingStore.SharedKernel/LzDetailingStore.SharedKernel.csproj src/LzDetailingStore.SharedKernel/

RUN dotnet restore src/LzDetailingStore.Api/LzDetailingStore.Api.csproj

# Copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/LzDetailingStore.Api/LzDetailingStore.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl: used by the compose healthcheck (the base image ships no wget/curl).
# libgssapi-krb5-2: Npgsql probes for GSSAPI at startup; without it the
# container logs "Cannot load library libgssapi_krb5.so.2".
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Run as the non-root user that the base image already provides (.NET 10 default: app).
USER $APP_UID

# The API listens on 8080 by default in .NET 10 containers.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LzDetailingStore.Api.dll"]
