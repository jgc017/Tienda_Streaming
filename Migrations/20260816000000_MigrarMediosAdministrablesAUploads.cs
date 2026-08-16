using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tienda_Streaming.Migrations
{
    public partial class MigrarMediosAdministrablesAUploads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "SistemaVisualConfig"
                SET "LogoUrl" = replace("LogoUrl", '/img/sistema/', '/uploads/sistema/'),
                    "FaviconUrl" = replace("FaviconUrl", '/img/sistema/', '/uploads/sistema/'),
                    "LoginBackgroundUrl" = replace("LoginBackgroundUrl", '/img/sistema/', '/uploads/sistema/'),
                    "VideoUrl" = replace("VideoUrl", '/video/sistema/', '/uploads/sistema/');

                UPDATE "InicioContenidos"
                SET "ImagenUrl" = replace("ImagenUrl", '/img/inicio/', '/uploads/inicio/');

                UPDATE "ImagenesProducto"
                SET "ImagenUrl" = replace("ImagenUrl", '/img/productos/', '/uploads/productos/');

                UPDATE "Combos"
                SET "ImagenUrl" = replace("ImagenUrl", '/img/combos/', '/uploads/combos/');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "SistemaVisualConfig"
                SET "LogoUrl" = replace("LogoUrl", '/uploads/sistema/', '/img/sistema/'),
                    "FaviconUrl" = replace("FaviconUrl", '/uploads/sistema/', '/img/sistema/'),
                    "LoginBackgroundUrl" = replace("LoginBackgroundUrl", '/uploads/sistema/', '/img/sistema/'),
                    "VideoUrl" = replace("VideoUrl", '/uploads/sistema/', '/video/sistema/');

                UPDATE "InicioContenidos"
                SET "ImagenUrl" = replace("ImagenUrl", '/uploads/inicio/', '/img/inicio/');

                UPDATE "ImagenesProducto"
                SET "ImagenUrl" = replace("ImagenUrl", '/uploads/productos/', '/img/productos/');

                UPDATE "Combos"
                SET "ImagenUrl" = replace("ImagenUrl", '/uploads/combos/', '/img/combos/');
                """);
        }
    }
}
