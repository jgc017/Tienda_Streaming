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
RUN apt-get update && apt-get install -y libkrb5-3

WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tienda_Streaming.dll"]