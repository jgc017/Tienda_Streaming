# -----------------------------
# STAGE 1: Build
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar todo el código
COPY . .

# Restaurar dependencias
RUN dotnet restore

# Publicar SOLO el proyecto principal (evita NETSDK1194)
RUN dotnet publish "Tienda_Streaming.csproj" -c Release -o /app

# -----------------------------
# STAGE 2: Runtime
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# ⭐ Librerías necesarias para MailKit (IMAP/SMTP, GSSAPI)
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    libkrb5-3

# ⭐ Directorio persistente para DataProtection
RUN mkdir -p /app/DataProtectionKeys

# ⭐ Eliminar watchers (inotify) y optimizar para contenedor
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copiar la app publicada
COPY --from=build /app .

# Puerto interno del contenedor
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Ejecutar la app
ENTRYPOINT ["dotnet", "Tienda_Streaming.dll"]