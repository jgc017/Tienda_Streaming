# -----------------------------
# STAGE 1: Build
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# -----------------------------
# STAGE 2: Runtime
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# ⭐ Necesario para IMAP con MailKit
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    libkrb5-3

WORKDIR /app

# ⭐ Desactivar watchers (inotify)
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tienda_Streaming.dll"]