# syntax=docker/dockerfile:1

# -----------------------------
# 1. Build stage (.NET SDK)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar primero el proyecto permite aprovechar cache de restore.
COPY ["Tienda_Streaming.csproj", "./"]
RUN dotnet restore "Tienda_Streaming.csproj"

# Copiar el resto del codigo y publicar para Linux.
COPY . .
RUN dotnet publish "Tienda_Streaming.csproj" \
    -c Release \
    -o /app/publish \
    --self-contained true \
    -r linux-x64 \
    /p:PublishSingleFile=true \
    /p:DebugType=None \
    /p:EnableCompressionInSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:PublishTrimmed=false

# -----------------------------
# 2. Runtime stage minimo
# -----------------------------
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
WORKDIR /app

# UID/GID no privilegiado. No se usa root para ejecutar la aplicacion.
ARG APP_UID=65532
ARG APP_GID=65532

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Directorios persistentes. DataProtection protege cookies y contrasenas de cuentas;
# wwwroot/img contiene imagenes cargadas desde los formularios administrativos.
RUN mkdir -p /app/App_Data/DataProtectionKeys /app/wwwroot/img \
    && chown -R ${APP_UID}:${APP_GID} /app

COPY --from=build --chown=${APP_UID}:${APP_GID} /app/publish .

USER ${APP_UID}:${APP_GID}

EXPOSE 8080
VOLUME ["/app/App_Data/DataProtectionKeys", "/app/wwwroot/img"]

# Healthcheck sin curl/wget: ejecuta la ruta liviana del binario.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD ["./Tienda_Streaming", "--healthcheck"]

ENTRYPOINT ["./Tienda_Streaming"]
