# Docker - Tienda Streaming

## Archivos incluidos

- `Dockerfile`: compila y publica la aplicacion .NET 10 para Linux con usuario no root.
- `docker-compose.yml`: levanta la aplicacion y PostgreSQL con volumenes persistentes.
- `docker.env.example`: plantilla de variables de entorno. Copiala como `.env` y cambia los secretos.

## Primer despliegue

```powershell
copy docker.env.example .env
```

Edita `.env` y cambia como minimo:

```text
POSTGRES_PASSWORD=UNA_PASSWORD_SEGURA
SMTP_USERNAME=...
SMTP_PASSWORD=...
SMTP_FROM_EMAIL=...
```

Luego levanta el sistema:

```powershell
docker compose --env-file .env up -d --build
```

La primera vez, `Database__ApplyMigrationsOnStartup=true` crea las tablas y datos iniciales desde la migracion limpia.

## Produccion con HTTPS

La aplicacion usa cookies seguras, CSRF seguro y redireccion HTTPS. En produccion debe quedar detras de un reverse proxy con certificado TLS, por ejemplo Nginx, Caddy, Traefik o IIS.

Configuracion recomendada en `.env` cuando uses reverse proxy:

```text
APP_BIND_ADDRESS=127.0.0.1
APP_HTTP_PORT=8080
TRUST_FORWARDED_HEADERS=true
REQUIRE_SECURE_COOKIES=true
```

Con eso el contenedor queda accesible solo desde el servidor local y el proxy publica HTTPS hacia internet. Para pruebas locales directas por HTTP en 127.0.0.1 puedes usar REQUIRE_SECURE_COOKIES=false en tu .env local; no lo uses asi expuesto a internet.

## Persistencia importante

No elimines estos volumenes sin respaldo:

- `postgres_data`: datos PostgreSQL.
- `app_dataprotection`: llaves DataProtection para descifrar cookies y contrasenas protegidas de cuentas.
- `app_images`: imagenes cargadas desde administracion.

Perder `app_dataprotection` puede impedir descifrar contrasenas de cuentas registradas previamente.

## Comandos utiles

Ver logs:

```powershell
docker compose --env-file .env logs -f tienda_streaming
```

Aplicar solo migraciones y salir:

```powershell
docker compose --env-file .env run --rm tienda_streaming --migrate
```

Detener servicios sin borrar datos:

```powershell
docker compose --env-file .env down
```

Detener y borrar volumenes, solo si quieres reiniciar todo desde cero:

```powershell
docker compose --env-file .env down -v
```
