# -----------------------------
# 1. Build stage (SDK)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos solo el csproj para cache
COPY ["Tienda_Streaming.csproj", "./"]
RUN dotnet restore "Tienda_Streaming.csproj"

# Copiamos el resto del cÃ³digo
COPY . .
WORKDIR "/src"

# PublicaciÃ³n estable (sin trimming agresivo)
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
# 2. Imagen final (Distroless)
# -----------------------------
FROM gcr.io/distroless/base-debian12 AS final

# Usuario no root
USER nonroot

WORKDIR /app

# Copiamos el ejecutable Ãºnico
COPY --from=build /app/publish .

# Healthcheck (opcional)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD ["./Tienda_Streaming", "--healthcheck"]

# Ejecutamos la app
ENTRYPOINT ["./Tienda_Streaming"]

