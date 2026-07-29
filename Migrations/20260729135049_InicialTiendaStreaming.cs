using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tienda_Streaming.Migrations
{
    /// <inheritdoc />
    public partial class InicialTiendaStreaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodigosCompra",
                columns: table => new
                {
                    Id_Codigo_Compra = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Valor_Inicial = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Saldo_Disponible = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Nombre_Cliente = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Correo_Cliente = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Fecha_Expiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosCompra", x => x.Id_Codigo_Compra);
                });

            migrationBuilder.CreateTable(
                name: "CorreosPlataforma",
                columns: table => new
                {
                    Id_Correo_Plataforma = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Hash_Mensaje = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Remitente = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Destinatarios = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Asunto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Encabezados = table.Column<string>(type: "text", nullable: true),
                    Cuerpo_Texto = table.Column<string>(type: "text", nullable: true),
                    Cuerpo_Html = table.Column<string>(type: "text", nullable: true),
                    Texto_Busqueda = table.Column<string>(type: "text", nullable: false),
                    Fecha_Recepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fecha_Registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosPlataforma", x => x.Id_Correo_Plataforma);
                });

            migrationBuilder.CreateTable(
                name: "Dominios",
                columns: table => new
                {
                    Id_Dominio = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Id_Padre = table.Column<int>(type: "integer", nullable: true),
                    DominioPadre = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "No"),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Crea = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dominios", x => x.Id_Dominio);
                    table.ForeignKey(
                        name: "FK_Dominios_Dominios_Id_Padre",
                        column: x => x.Id_Padre,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InicioContenidos",
                columns: table => new
                {
                    Id_InicioContenido = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoContenido = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Resumen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Contenido = table.Column<string>(type: "text", nullable: true),
                    ImagenUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnlaceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TextoBoton = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    MostrarEnInicio = table.Column<short>(type: "smallint", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InicioContenidos", x => x.Id_InicioContenido);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id_Menu = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Id_Padre = table.Column<int>(type: "integer", nullable: true),
                    Posicion = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Controlador = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Vista = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Icono = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id_Menu);
                    table.ForeignKey(
                        name: "FK_Menus_Menus_Id_Padre",
                        column: x => x.Id_Padre,
                        principalTable: "Menus",
                        principalColumn: "Id_Menu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id_Rol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rol = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id_Rol);
                });

            migrationBuilder.CreateTable(
                name: "SistemaVisualConfig",
                columns: table => new
                {
                    Id_SistemaVisualConfig = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FaviconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LoginBackgroundUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemaVisualConfig", x => x.Id_SistemaVisualConfig);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    E_Mail = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Password = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Debe_Cambiar_Password = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id_Usuario);
                });

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    Id_Combo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImagenUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Id_Tipo_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Tiempo_Pantalla = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.Id_Combo);
                    table.ForeignKey(
                        name: "FK_Combos_Dominios_Id_Tipo_Usuario",
                        column: x => x.Id_Tipo_Usuario,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id_Cuenta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Plataforma = table.Column<int>(type: "integer", nullable: false),
                    Id_Tipo_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Tiempo_Pantalla = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    Correo_Cuenta = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Contraseña_Cuenta = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Perfil_Cuenta = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Pin_Cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Fecha_Vencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id_Cuenta);
                    table.ForeignKey(
                        name: "FK_Cuentas_Dominios_Id_Plataforma",
                        column: x => x.Id_Plataforma,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cuentas_Dominios_Id_Tipo_Usuario",
                        column: x => x.Id_Tipo_Usuario,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagenesProducto",
                columns: table => new
                {
                    Id_ImagenProducto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Plataforma = table.Column<int>(type: "integer", nullable: false),
                    Id_Tipo_Imagen = table.Column<int>(type: "integer", nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ImagenUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagenesProducto", x => x.Id_ImagenProducto);
                    table.ForeignKey(
                        name: "FK_ImagenesProducto_Dominios_Id_Plataforma",
                        column: x => x.Id_Plataforma,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImagenesProducto_Dominios_Id_Tipo_Imagen",
                        column: x => x.Id_Tipo_Imagen,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreciosProducto",
                columns: table => new
                {
                    Id_Precio_Producto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Plataforma = table.Column<int>(type: "integer", nullable: false),
                    Id_Tipo_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Tiempo_Pantalla = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciosProducto", x => x.Id_Precio_Producto);
                    table.ForeignKey(
                        name: "FK_PreciosProducto_Dominios_Id_Plataforma",
                        column: x => x.Id_Plataforma,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreciosProducto_Dominios_Id_Tipo_Usuario",
                        column: x => x.Id_Tipo_Usuario,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id_Permiso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Menu = table.Column<int>(type: "integer", nullable: true),
                    TipoPermiso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Menu"),
                    Modulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Accion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Controlador = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Metodo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HttpMetodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CodigoPermiso = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id_Permiso);
                    table.ForeignKey(
                        name: "FK_Permisos_Menus_Id_Menu",
                        column: x => x.Id_Menu,
                        principalTable: "Menus",
                        principalColumn: "Id_Menu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id_Auditoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Formulario = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Metodo_Ejecutado = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id_Auditoria);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuarios_Id_Usuario_Creacion",
                        column: x => x.Id_Usuario_Creacion,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuarios_Id_Usuario_Modifica",
                        column: x => x.Id_Usuario_Modifica,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BilleteraVendedores",
                columns: table => new
                {
                    Id_Billetera = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Saldo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BilleteraVendedores", x => x.Id_Billetera);
                    table.ForeignKey(
                        name: "FK_BilleteraVendedores_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id_PasswordResetToken = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Usuario = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fecha_Expiracion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fecha_Uso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ip_Solicitud = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id_PasswordResetToken);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Id_Tipo_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Id_Usuario = table.Column<int>(type: "integer", nullable: true),
                    Id_Codigo_Compra = table.Column<int>(type: "integer", nullable: true),
                    Nombre_Cliente = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Correo_Cliente = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Fecha_Compra = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id_Pedido);
                    table.ForeignKey(
                        name: "FK_Pedidos_CodigosCompra_Id_Codigo_Compra",
                        column: x => x.Id_Codigo_Compra,
                        principalTable: "CodigosCompra",
                        principalColumn: "Id_Codigo_Compra",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Dominios_Id_Tipo_Usuario",
                        column: x => x.Id_Tipo_Usuario,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles_User",
                columns: table => new
                {
                    Id_Roles_User = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Usuario = table.Column<int>(type: "integer", nullable: false),
                    Id_Rol = table.Column<int>(type: "integer", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles_User", x => x.Id_Roles_User);
                    table.ForeignKey(
                        name: "FK_Roles_User_Roles_Id_Rol",
                        column: x => x.Id_Rol,
                        principalTable: "Roles",
                        principalColumn: "Id_Rol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roles_User_Usuarios_Id_Usuario",
                        column: x => x.Id_Usuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id_Usuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComboPlataformas",
                columns: table => new
                {
                    Id_Combo_Plataforma = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Combo = table.Column<int>(type: "integer", nullable: false),
                    Id_Plataforma = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboPlataformas", x => x.Id_Combo_Plataforma);
                    table.ForeignKey(
                        name: "FK_ComboPlataformas_Combos_Id_Combo",
                        column: x => x.Id_Combo,
                        principalTable: "Combos",
                        principalColumn: "Id_Combo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboPlataformas_Dominios_Id_Plataforma",
                        column: x => x.Id_Plataforma,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles_Permisos",
                columns: table => new
                {
                    Id_Rol_Permiso = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Rol = table.Column<int>(type: "integer", nullable: false),
                    Id_Permiso = table.Column<int>(type: "integer", nullable: false),
                    Vigente = table.Column<short>(type: "smallint", nullable: false),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true),
                    Id_Usuario_Modifica = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Modifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Maquina_Modifica = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles_Permisos", x => x.Id_Rol_Permiso);
                    table.ForeignKey(
                        name: "FK_Roles_Permisos_Permisos_Id_Permiso",
                        column: x => x.Id_Permiso,
                        principalTable: "Permisos",
                        principalColumn: "Id_Permiso",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Roles_Permisos_Roles_Id_Rol",
                        column: x => x.Id_Rol,
                        principalTable: "Roles",
                        principalColumn: "Id_Rol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosBilletera",
                columns: table => new
                {
                    Id_Movimiento_Billetera = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Billetera = table.Column<int>(type: "integer", nullable: false),
                    Tipo_Movimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Saldo_Anterior = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Saldo_Nuevo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: true),
                    Id_Usuario_Creacion = table.Column<int>(type: "integer", nullable: true),
                    Fecha_Creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Maquina_Creacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosBilletera", x => x.Id_Movimiento_Billetera);
                    table.ForeignKey(
                        name: "FK_MovimientosBilletera_BilleteraVendedores_Id_Billetera",
                        column: x => x.Id_Billetera,
                        principalTable: "BilleteraVendedores",
                        principalColumn: "Id_Billetera",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosBilletera_Pedidos_Id_Pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id_Pedido",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoDetalles",
                columns: table => new
                {
                    Id_Pedido_Detalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Tipo_Producto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Id_Plataforma = table.Column<int>(type: "integer", nullable: true),
                    Id_Combo = table.Column<int>(type: "integer", nullable: true),
                    Tiempo_Pantalla = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio_Unitario = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoDetalles", x => x.Id_Pedido_Detalle);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Combos_Id_Combo",
                        column: x => x.Id_Combo,
                        principalTable: "Combos",
                        principalColumn: "Id_Combo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Dominios_Id_Plataforma",
                        column: x => x.Id_Plataforma,
                        principalTable: "Dominios",
                        principalColumn: "Id_Dominio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoDetalles_Pedidos_Id_Pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id_Pedido",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoCuentas",
                columns: table => new
                {
                    Id_Pedido_Cuenta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Id_Cuenta = table.Column<int>(type: "integer", nullable: false),
                    Id_Pedido_Detalle = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoCuentas", x => x.Id_Pedido_Cuenta);
                    table.ForeignKey(
                        name: "FK_PedidoCuentas_Cuentas_Id_Cuenta",
                        column: x => x.Id_Cuenta,
                        principalTable: "Cuentas",
                        principalColumn: "Id_Cuenta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoCuentas_PedidoDetalles_Id_Pedido_Detalle",
                        column: x => x.Id_Pedido_Detalle,
                        principalTable: "PedidoDetalles",
                        principalColumn: "Id_Pedido_Detalle",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoCuentas_Pedidos_Id_Pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedidos",
                        principalColumn: "Id_Pedido",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Id_Usuario_Creacion",
                table: "Auditoria",
                column: "Id_Usuario_Creacion");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Id_Usuario_Modifica",
                table: "Auditoria",
                column: "Id_Usuario_Modifica");

            migrationBuilder.CreateIndex(
                name: "IX_BilleteraVendedores_Id_Usuario",
                table: "BilleteraVendedores",
                column: "Id_Usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodigosCompra_Codigo",
                table: "CodigosCompra",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboPlataformas_Id_Combo_Id_Plataforma",
                table: "ComboPlataformas",
                columns: new[] { "Id_Combo", "Id_Plataforma" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboPlataformas_Id_Plataforma",
                table: "ComboPlataformas",
                column: "Id_Plataforma");

            migrationBuilder.CreateIndex(
                name: "IX_Combos_Id_Tipo_Usuario",
                table: "Combos",
                column: "Id_Tipo_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Combos_Nombre_Id_Tipo_Usuario_Tiempo_Pantalla",
                table: "Combos",
                columns: new[] { "Nombre", "Id_Tipo_Usuario", "Tiempo_Pantalla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorreosPlataforma_Fecha_Recepcion",
                table: "CorreosPlataforma",
                column: "Fecha_Recepcion");

            migrationBuilder.CreateIndex(
                name: "IX_CorreosPlataforma_Hash_Mensaje",
                table: "CorreosPlataforma",
                column: "Hash_Mensaje",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Id_Plataforma_Id_Tipo_Usuario_Vigente",
                table: "Cuentas",
                columns: new[] { "Id_Plataforma", "Id_Tipo_Usuario", "Vigente" });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Id_Tipo_Usuario",
                table: "Cuentas",
                column: "Id_Tipo_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Dominios_Id_Padre",
                table: "Dominios",
                column: "Id_Padre");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_Id_Plataforma_Id_Tipo_Imagen",
                table: "ImagenesProducto",
                columns: new[] { "Id_Plataforma", "Id_Tipo_Imagen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_Id_Tipo_Imagen",
                table: "ImagenesProducto",
                column: "Id_Tipo_Imagen");

            migrationBuilder.CreateIndex(
                name: "IX_ImagenesProducto_Orden",
                table: "ImagenesProducto",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "IX_InicioContenidos_TipoContenido_Orden",
                table: "InicioContenidos",
                columns: new[] { "TipoContenido", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Controlador_Vista",
                table: "Menus",
                columns: new[] { "Controlador", "Vista" });

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Id_Padre",
                table: "Menus",
                column: "Id_Padre");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosBilletera_Id_Billetera",
                table: "MovimientosBilletera",
                column: "Id_Billetera");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosBilletera_Id_Pedido",
                table: "MovimientosBilletera",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Id_Usuario_Fecha_Expiracion",
                table: "PasswordResetTokens",
                columns: new[] { "Id_Usuario", "Fecha_Expiracion" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCuentas_Id_Cuenta",
                table: "PedidoCuentas",
                column: "Id_Cuenta");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCuentas_Id_Pedido_Detalle",
                table: "PedidoCuentas",
                column: "Id_Pedido_Detalle");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoCuentas_Id_Pedido_Id_Cuenta",
                table: "PedidoCuentas",
                columns: new[] { "Id_Pedido", "Id_Cuenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_Id_Combo",
                table: "PedidoDetalles",
                column: "Id_Combo");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_Id_Pedido",
                table: "PedidoDetalles",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoDetalles_Id_Plataforma",
                table: "PedidoDetalles",
                column: "Id_Plataforma");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Id_Codigo_Compra",
                table: "Pedidos",
                column: "Id_Codigo_Compra");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Id_Tipo_Usuario",
                table: "Pedidos",
                column: "Id_Tipo_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Id_Usuario",
                table: "Pedidos",
                column: "Id_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_CodigoPermiso",
                table: "Permisos",
                column: "CodigoPermiso",
                unique: true,
                filter: "\"CodigoPermiso\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Id_Menu",
                table: "Permisos",
                column: "Id_Menu");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_TipoPermiso_Id_Menu_Accion",
                table: "Permisos",
                columns: new[] { "TipoPermiso", "Id_Menu", "Accion" },
                unique: true,
                filter: "\"Id_Menu\" IS NOT NULL AND \"TipoPermiso\" = 'Menu'");

            migrationBuilder.CreateIndex(
                name: "IX_PreciosProducto_Id_Plataforma_Id_Tipo_Usuario_Tiempo_Pantal~",
                table: "PreciosProducto",
                columns: new[] { "Id_Plataforma", "Id_Tipo_Usuario", "Tiempo_Pantalla" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreciosProducto_Id_Tipo_Usuario",
                table: "PreciosProducto",
                column: "Id_Tipo_Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Rol",
                table: "Roles",
                column: "Rol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Permisos_Id_Permiso",
                table: "Roles_Permisos",
                column: "Id_Permiso");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Permisos_Id_Rol_Id_Permiso",
                table: "Roles_Permisos",
                columns: new[] { "Id_Rol", "Id_Permiso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_User_Id_Rol",
                table: "Roles_User",
                column: "Id_Rol");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_User_Id_Usuario_Id_Rol",
                table: "Roles_User",
                columns: new[] { "Id_Usuario", "Id_Rol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_E_Mail",
                table: "Usuarios",
                column: "E_Mail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Usuario",
                table: "Usuarios",
                column: "Usuario",
                unique: true);

            SembrarDatosIniciales(migrationBuilder);
        }

        private static void SembrarDatosIniciales(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Roles" ("Id_Rol", "Rol", "Vigente", "Fecha_Creacion", "Maquina_Creacion") VALUES
                (1, 'Super Usuario', 1, now(), 'MigracionInicial'),
                (2, 'Administrador', 1, now(), 'MigracionInicial'),
                (3, 'Vendedor', 1, now(), 'MigracionInicial');

                INSERT INTO "Dominios" ("Id_Dominio", "Descripcion", "Id_Padre", "Vigente", "Fecha_Creacion", "Maquina_Creacion", "DominioPadre") VALUES
                (1, 'SIN DATOS', NULL, 1, now(), 'MigracionInicial', 'Si'),
                (2, 'Dominio', 1, 1, now(), 'MigracionInicial', 'Si'),
                (3, 'Permisos', 2, 1, now(), 'MigracionInicial', 'Si'),
                (4, 'Ver', 3, 1, now(), 'MigracionInicial', 'No'),
                (5, 'Crear', 3, 1, now(), 'MigracionInicial', 'No'),
                (6, 'Consultar', 3, 1, now(), 'MigracionInicial', 'No'),
                (7, 'Actualizar', 3, 1, now(), 'MigracionInicial', 'No'),
                (8, 'Eliminar', 3, 1, now(), 'MigracionInicial', 'No'),
                (9, 'Asignar', 3, 1, now(), 'MigracionInicial', 'No'),
                (10, 'Plataforma', 2, 1, now(), 'MigracionInicial', 'Si'),
                (11, 'Netflix', 10, 1, now(), 'MigracionInicial', 'No'),
                (12, 'HBO Max', 10, 1, now(), 'MigracionInicial', 'No'),
                (13, 'Prime Video', 10, 1, now(), 'MigracionInicial', 'No'),
                (14, 'Paramount', 10, 1, now(), 'MigracionInicial', 'No'),
                (15, 'Disney Premium', 10, 1, now(), 'MigracionInicial', 'No'),
                (16, 'Disney Basic', 10, 1, now(), 'MigracionInicial', 'No'),
                (17, 'Vix', 10, 1, now(), 'MigracionInicial', 'No'),
                (18, 'Crunchyroll', 10, 1, now(), 'MigracionInicial', 'No'),
                (19, 'Netflix Extranjera', 10, 1, now(), 'MigracionInicial', 'No'),
                (20, 'Spotify', 10, 1, now(), 'MigracionInicial', 'No'),
                (21, 'Youtube', 10, 1, now(), 'MigracionInicial', 'No'),
                (22, 'Tipo Usuario', 2, 1, now(), 'MigracionInicial', 'Si'),
                (23, 'Cliente', 22, 1, now(), 'MigracionInicial', 'No'),
                (24, 'Vendedor', 22, 1, now(), 'MigracionInicial', 'No'),
                (25, 'TipoContenidoInicio', 2, 1, now(), 'MigracionInicial', 'Si'),
                (26, 'Slider', 25, 1, now(), 'MigracionInicial', 'No'),
                (27, 'Contacto', 25, 1, now(), 'MigracionInicial', 'No'),
                (34, 'Tipo Imagen', 2, 1, now(), 'MigracionInicial', 'Si'),
                (35, 'Pantalla Individual', 34, 1, now(), 'MigracionInicial', 'No'),
                (36, 'Combo', 34, 1, now(), 'MigracionInicial', 'No');

                INSERT INTO "Menus" ("Id_Menu", "Descripcion", "Id_Padre", "Posicion", "Tipo", "Controlador", "Vista", "Icono", "Vigente", "Fecha_Creacion", "Maquina_Creacion") VALUES
                (1, 'Administracion', NULL, 2, 'Modulo', NULL, NULL, 'fa-solid fa-gear', 1, now(), 'MigracionInicial'),
                (2, 'Usuarios', 1, 1, 'Formulario', 'Usuarios', 'VwUsuarios', 'fa-solid fa-users', 1, now(), 'MigracionInicial'),
                (3, 'Roles', 1, 2, 'Formulario', 'Roles', 'VwRoles', 'fa-solid fa-user-shield', 1, now(), 'MigracionInicial'),
                (4, 'Permisos', 1, 3, 'Formulario', 'Permisos', 'VwPermisos', 'fa-solid fa-key', 1, now(), 'MigracionInicial'),
                (5, 'Dominios', 1, 4, 'Formulario', 'Dominios', 'VwDominios', 'fa-solid fa-sitemap', 1, now(), 'MigracionInicial'),
                (6, 'Agregar Productos', NULL, 3, 'Modulo', NULL, NULL, 'fa-solid fa-house-chimney-window', 1, now(), 'MigracionInicial'),
                (7, 'Imagenes y Videos', 1, 6, 'Formulario', 'SistemaConfig', 'VwSistemaConfig', 'fa-solid fa-image', 1, now(), 'MigracionInicial'),
                (8, 'Tiendas', NULL, 1, 'Formulario', 'Tiendas', 'VwTiendas', 'fa-solid fa-store', 1, now(), 'MigracionInicial'),
                (9, 'Registrar Publicaciones', 6, 1, 'Formulario', 'RegistrarPublicaciones', 'VwRegistrarPublicaciones', 'fa-solid fa-newspaper', 1, now(), 'MigracionInicial'),
                (10, 'Registrar Productos', 6, 2, 'Formulario', 'RegistrarProductos', 'VwRegistrarProductos', 'fa-solid fa-boxes-stacked', 1, now(), 'MigracionInicial'),
                (11, 'Imagenes Producto', 6, 3, 'Formulario', 'ImagenesProducto', 'VwImagenesProducto', 'fa-solid fa-image', 1, now(), 'MigracionInicial'),
                (12, 'Registrar Precios', 6, 4, 'Formulario', 'RegistrarPrecios', 'VwRegistrarPrecios', 'fa-solid fa-tags', 1, now(), 'MigracionInicial'),
                (13, 'Registrar Combos', 6, 5, 'Formulario', 'RegistrarCombos', 'VwRegistrarCombos', 'fa-solid fa-layer-group', 1, now(), 'MigracionInicial'),
                (14, 'Billetera Vendedores', 6, 6, 'Formulario', 'BilleteraVendedores', 'VwBilleteraVendedores', 'fa-solid fa-wallet', 1, now(), 'MigracionInicial'),
                (15, 'Codigos Compra', 6, 7, 'Formulario', 'CodigosCompra', 'VwCodigosCompra', 'fa-solid fa-ticket', 1, now(), 'MigracionInicial'),
                (16, 'Historial Compras', 6, 8, 'Formulario', 'HistorialCompras', 'VwHistorialCompras', 'fa-solid fa-clock-rotate-left', 1, now(), 'MigracionInicial'),
                (17, 'Administracion Correos', NULL, 4, 'Modulo', NULL, NULL, 'fa-solid fa-envelopes-bulk', 1, now(), 'MigracionInicial'),
                (18, 'Codigos Plataformas', 17, 1, 'Formulario', 'AdministracionCorreos', 'VwCodigosPlataformas', 'fa-solid fa-envelope-open-text', 1, now(), 'MigracionInicial');

                INSERT INTO "SistemaVisualConfig" ("Id_SistemaVisualConfig", "LogoUrl", "FaviconUrl", "LoginBackgroundUrl", "VideoUrl", "Vigente", "Fecha_Creacion", "Maquina_Creacion") VALUES
                (1, '/img/sistema/logo_20260722004923_478d1f5c87ad499db5dcdd1a3cadc0d9.png', '/img/sistema/favicon_20260722004933_6e85d8407b7940afbdb358d96c32110a.png', '/img/sistema/loginbackground_20260722004943_872c15bb659e4aa9a860bc33bc7e449b.png', NULL, 1, now(), 'MigracionInicial');

                INSERT INTO "Permisos" ("Id_Permiso", "TipoPermiso", "Id_Menu", "Modulo", "Accion", "Descripcion", "Controlador", "Metodo", "HttpMetodo", "CodigoPermiso", "Vigente", "Fecha_Creacion", "Maquina_Creacion") VALUES
                (1, 'Menu', 2, 'Usuarios', 'Ver', 'Ver Usuarios', 'Usuarios', 'VwUsuarios', 'GET', 'Menu:Usuarios:VwUsuarios:Ver', 1, now(), 'MigracionInicial'),
                (2, 'Menu', 3, 'Roles', 'Ver', 'Ver Roles', 'Roles', 'VwRoles', 'GET', 'Menu:Roles:VwRoles:Ver', 1, now(), 'MigracionInicial'),
                (3, 'Menu', 4, 'Permisos', 'Ver', 'Ver Permisos', 'Permisos', 'VwPermisos', 'GET', 'Menu:Permisos:VwPermisos:Ver', 1, now(), 'MigracionInicial'),
                (4, 'Menu', 5, 'Dominios', 'Ver', 'Ver Dominios', 'Dominios', 'VwDominios', 'GET', 'Menu:Dominios:VwDominios:Ver', 1, now(), 'MigracionInicial'),
                (5, 'Menu', 7, 'SistemaConfig', 'Ver', 'Ver Imagenes y Videos', 'SistemaConfig', 'VwSistemaConfig', 'GET', 'Menu:SistemaConfig:VwSistemaConfig:Ver', 1, now(), 'MigracionInicial'),
                (6, 'Menu', 8, 'Tiendas', 'Ver', 'Ver Tiendas', 'Tiendas', 'VwTiendas', 'GET', 'Menu:Tiendas:VwTiendas:Ver', 1, now(), 'MigracionInicial'),
                (7, 'Menu', 9, 'RegistrarPublicaciones', 'Ver', 'Ver Registrar Publicaciones', 'RegistrarPublicaciones', 'VwRegistrarPublicaciones', 'GET', 'Menu:RegistrarPublicaciones:VwRegistrarPublicaciones:Ver', 1, now(), 'MigracionInicial'),
                (8, 'Menu', 10, 'RegistrarProductos', 'Ver', 'Ver Registrar Productos', 'RegistrarProductos', 'VwRegistrarProductos', 'GET', 'Menu:RegistrarProductos:VwRegistrarProductos:Ver', 1, now(), 'MigracionInicial'),
                (9, 'Menu', 11, 'ImagenesProducto', 'Ver', 'Ver Imagenes Producto', 'ImagenesProducto', 'VwImagenesProducto', 'GET', 'Menu:ImagenesProducto:VwImagenesProducto:Ver', 1, now(), 'MigracionInicial'),
                (10, 'Menu', 12, 'RegistrarPrecios', 'Ver', 'Ver Registrar Precios', 'RegistrarPrecios', 'VwRegistrarPrecios', 'GET', 'Menu:RegistrarPrecios:VwRegistrarPrecios:Ver', 1, now(), 'MigracionInicial'),
                (11, 'Menu', 13, 'RegistrarCombos', 'Ver', 'Ver Registrar Combos', 'RegistrarCombos', 'VwRegistrarCombos', 'GET', 'Menu:RegistrarCombos:VwRegistrarCombos:Ver', 1, now(), 'MigracionInicial'),
                (12, 'Menu', 14, 'BilleteraVendedores', 'Ver', 'Ver Billetera Vendedores', 'BilleteraVendedores', 'VwBilleteraVendedores', 'GET', 'Menu:BilleteraVendedores:VwBilleteraVendedores:Ver', 1, now(), 'MigracionInicial'),
                (13, 'Menu', 15, 'CodigosCompra', 'Ver', 'Ver Codigos Compra', 'CodigosCompra', 'VwCodigosCompra', 'GET', 'Menu:CodigosCompra:VwCodigosCompra:Ver', 1, now(), 'MigracionInicial'),
                (14, 'Menu', 16, 'HistorialCompras', 'Ver', 'Ver Historial Compras', 'HistorialCompras', 'VwHistorialCompras', 'GET', 'Menu:HistorialCompras:VwHistorialCompras:Ver', 1, now(), 'MigracionInicial'),
                (15, 'Menu', 18, 'AdministracionCorreos', 'Ver', 'Ver Codigos Plataformas', 'AdministracionCorreos', 'VwCodigosPlataformas', 'GET', 'Menu:AdministracionCorreos:VwCodigosPlataformas:Ver', 1, now(), 'MigracionInicial'),
                (16, 'Metodo', 14, 'BilleteraVendedores', 'Consultar', 'Permite consultar el detalle de registros del modulo Billetera Vendedores.', 'BilleteraVendedoresApi', 'F_GetBilletera', 'GET', 'BILLETERAVENDEDORESAPI.F_GETBILLETERA.GET', 1, now(), 'MigracionInicial'),
                (17, 'Metodo', 14, 'BilleteraVendedores', 'Crear', 'Permite registrar nueva informacion en el modulo Billetera Vendedores.', 'BilleteraVendedoresApi', 'P_RecargarBilletera', 'POST', 'BILLETERAVENDEDORESAPI.P_RECARGARBILLETERA.POST', 1, now(), 'MigracionInicial'),
                (18, 'Metodo', 14, 'BilleteraVendedores', 'Actualizar', 'Permite modificar informacion existente del modulo Billetera Vendedores.', 'BilleteraVendedoresApi', 'P_UdpBilletera', 'PUT', 'BILLETERAVENDEDORESAPI.P_UDPBILLETERA.PUT', 1, now(), 'MigracionInicial'),
                (19, 'Metodo', 15, 'CodigosCompra', 'Consultar', 'Permite consultar el detalle de registros del modulo Codigos Compra.', 'CodigosCompraApi', 'F_GetCodigoCompra', 'GET', 'CODIGOSCOMPRAAPI.F_GETCODIGOCOMPRA.GET', 1, now(), 'MigracionInicial'),
                (20, 'Metodo', 15, 'CodigosCompra', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Codigos Compra.', 'CodigosCompraApi', 'P_DeleteCodigoCompra', 'DELETE', 'CODIGOSCOMPRAAPI.P_DELETECODIGOCOMPRA.DELETE', 1, now(), 'MigracionInicial'),
                (21, 'Metodo', 15, 'CodigosCompra', 'Crear', 'Permite registrar nueva informacion en el modulo Codigos Compra.', 'CodigosCompraApi', 'P_GenerarCodigoCompra', 'POST', 'CODIGOSCOMPRAAPI.P_GENERARCODIGOCOMPRA.POST', 1, now(), 'MigracionInicial'),
                (22, 'Metodo', 15, 'CodigosCompra', 'Actualizar', 'Permite modificar informacion existente del modulo Codigos Compra.', 'CodigosCompraApi', 'P_UdpCodigoCompra', 'PUT', 'CODIGOSCOMPRAAPI.P_UDPCODIGOCOMPRA.PUT', 1, now(), 'MigracionInicial'),
                (23, 'Metodo', 18, 'CodigosPlataformas', 'Consultar', 'Permite consultar el detalle de registros del modulo Codigos Plataformas.', 'CodigosPlataformasApi', 'F_GetCorreoDetalle', 'GET', 'CODIGOSPLATAFORMASAPI.F_GETCORREODETALLE.GET', 1, now(), 'MigracionInicial'),
                (24, 'Metodo', 18, 'CodigosPlataformas', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Codigos Plataformas.', 'CodigosPlataformasApi', 'P_DeleteCorreo', 'DELETE', 'CODIGOSPLATAFORMASAPI.P_DELETECORREO.DELETE', 1, now(), 'MigracionInicial'),
                (25, 'Metodo', 18, 'CodigosPlataformas', 'Crear', 'Permite registrar nueva informacion en el modulo Codigos Plataformas.', 'CodigosPlataformasApi', 'P_SincronizarBuzon', 'POST', 'CODIGOSPLATAFORMASAPI.P_SINCRONIZARBUZON.POST', 1, now(), 'MigracionInicial'),
                (26, 'Metodo', 5, 'Dominios', 'Consultar', 'Permite consultar el detalle de registros del modulo Dominios.', 'DominiosApi', 'F_GetDominio', 'GET', 'DOMINIOSAPI.F_GETDOMINIO.GET', 1, now(), 'MigracionInicial'),
                (27, 'Metodo', 5, 'Dominios', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Dominios.', 'DominiosApi', 'P_DeleteDominio', 'DELETE', 'DOMINIOSAPI.P_DELETEDOMINIO.DELETE', 1, now(), 'MigracionInicial'),
                (28, 'Metodo', 5, 'Dominios', 'Crear', 'Permite registrar nueva informacion en el modulo Dominios.', 'DominiosApi', 'P_InsDominio', 'POST', 'DOMINIOSAPI.P_INSDOMINIO.POST', 1, now(), 'MigracionInicial'),
                (29, 'Metodo', 5, 'Dominios', 'Actualizar', 'Permite modificar informacion existente del modulo Dominios.', 'DominiosApi', 'P_UdpDominio', 'PUT', 'DOMINIOSAPI.P_UDPDOMINIO.PUT', 1, now(), 'MigracionInicial'),
                (30, 'Metodo', 16, 'HistorialCompras', 'Consultar', 'Permite consultar el detalle de registros del modulo Historial Compras.', 'HistorialComprasApi', 'F_GetDetalleCompra', 'GET', 'HISTORIALCOMPRASAPI.F_GETDETALLECOMPRA.GET', 1, now(), 'MigracionInicial'),
                (31, 'Metodo', 16, 'HistorialCompras', 'Consultar', 'Permite consultar el detalle de registros del modulo Historial Compras.', 'HistorialComprasApi', 'F_GetHistorialCompras', 'GET', 'HISTORIALCOMPRASAPI.F_GETHISTORIALCOMPRAS.GET', 1, now(), 'MigracionInicial'),
                (32, 'Metodo', 11, 'ImagenesProducto', 'Consultar', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'F_GetImagenProducto', 'GET', 'IMAGENESPRODUCTOAPI.F_GETIMAGENPRODUCTO.GET', 1, now(), 'MigracionInicial'),
                (33, 'Metodo', 11, 'ImagenesProducto', 'Eliminar', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'P_DeleteImagenProducto', 'DELETE', 'IMAGENESPRODUCTOAPI.P_DELETEIMAGENPRODUCTO.DELETE', 1, now(), 'MigracionInicial'),
                (34, 'Metodo', 11, 'ImagenesProducto', 'Crear', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'P_InsImagenProducto', 'POST', 'IMAGENESPRODUCTOAPI.P_INSIMAGENPRODUCTO.POST', 1, now(), 'MigracionInicial'),
                (35, 'Metodo', 11, 'ImagenesProducto', 'Crear', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'P_MoverImagenProducto', 'POST', 'IMAGENESPRODUCTOAPI.P_MOVERIMAGENPRODUCTO.POST', 1, now(), 'MigracionInicial'),
                (36, 'Metodo', 11, 'ImagenesProducto', 'Actualizar', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'P_UdpImagenProducto', 'PUT', 'IMAGENESPRODUCTOAPI.P_UDPIMAGENPRODUCTO.PUT', 1, now(), 'MigracionInicial'),
                (37, 'Metodo', 11, 'ImagenesProducto', 'Cargar', 'Permite cargar archivos o imagenes del modulo Imagenes Producto.', 'ImagenesProductoApi', 'P_UploadImagenProducto', 'POST', 'IMAGENESPRODUCTOAPI.P_UPLOADIMAGENPRODUCTO.POST', 1, now(), 'MigracionInicial'),
                (38, 'Metodo', 4, 'Permisos', 'Consultar', 'Permite consultar el detalle de registros del modulo Permisos.', 'PermisosApi', 'F_GetPermiso', 'GET', 'PERMISOSAPI.F_GETPERMISO.GET', 1, now(), 'MigracionInicial'),
                (39, 'Metodo', 4, 'Permisos', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Permisos.', 'PermisosApi', 'F_GetPermisoRol', 'GET', 'PERMISOSAPI.F_GETPERMISOROL.GET', 1, now(), 'MigracionInicial'),
                (40, 'Metodo', 4, 'Permisos', 'Consultar', 'Permite consultar el detalle de registros del modulo Permisos.', 'PermisosApi', 'F_GetRolesPorPermiso', 'GET', 'PERMISOSAPI.F_GETROLESPORPERMISO.GET', 1, now(), 'MigracionInicial'),
                (41, 'Metodo', 4, 'Permisos', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Permisos.', 'PermisosApi', 'P_DeletePermiso', 'DELETE', 'PERMISOSAPI.P_DELETEPERMISO.DELETE', 1, now(), 'MigracionInicial'),
                (42, 'Metodo', 4, 'Permisos', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Permisos.', 'PermisosApi', 'P_DeletePermisoRol', 'DELETE', 'PERMISOSAPI.P_DELETEPERMISOROL.DELETE', 1, now(), 'MigracionInicial'),
                (43, 'Metodo', 4, 'Permisos', 'Crear', 'Permite registrar nueva informacion en el modulo Permisos.', 'PermisosApi', 'P_InsPermiso', 'POST', 'PERMISOSAPI.P_INSPERMISO.POST', 1, now(), 'MigracionInicial'),
                (44, 'Metodo', 4, 'Permisos', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Permisos.', 'PermisosApi', 'P_InsPermisoRol', 'POST', 'PERMISOSAPI.P_INSPERMISOROL.POST', 1, now(), 'MigracionInicial'),
                (45, 'Metodo', 4, 'Permisos', 'Actualizar', 'Permite modificar informacion existente del modulo Permisos.', 'PermisosApi', 'P_UdpPermiso', 'PUT', 'PERMISOSAPI.P_UDPPERMISO.PUT', 1, now(), 'MigracionInicial'),
                (46, 'Metodo', 4, 'Permisos', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Permisos.', 'PermisosApi', 'P_UdpRolesPermiso', 'PUT', 'PERMISOSAPI.P_UDPROLESPERMISO.PUT', 1, now(), 'MigracionInicial'),
                (47, 'Metodo', 4, 'Permisos', 'Consultar', 'Permite consultar el detalle de registros del modulo Permisos.', 'PermisosMetodosApi', 'F_GetPermisoMetodo', 'GET', 'PERMISOSMETODOSAPI.F_GETPERMISOMETODO.GET', 1, now(), 'MigracionInicial'),
                (48, 'Metodo', 4, 'Permisos', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Permisos.', 'PermisosMetodosApi', 'P_DeletePermisoMetodo', 'DELETE', 'PERMISOSMETODOSAPI.P_DELETEPERMISOMETODO.DELETE', 1, now(), 'MigracionInicial'),
                (49, 'Metodo', 4, 'Permisos', 'Sincronizar', 'Permite sincronizar informacion automatica del modulo Permisos.', 'PermisosMetodosApi', 'P_SyncPermisosMetodos', 'POST', 'PERMISOSMETODOSAPI.P_SYNCPERMISOSMETODOS.POST', 1, now(), 'MigracionInicial'),
                (50, 'Metodo', 4, 'Permisos', 'Actualizar', 'Permite modificar informacion existente del modulo Permisos.', 'PermisosMetodosApi', 'P_UdpPermisoMetodo', 'PUT', 'PERMISOSMETODOSAPI.P_UDPPERMISOMETODO.PUT', 1, now(), 'MigracionInicial'),
                (51, 'Metodo', 13, 'RegistrarCombos', 'Consultar', 'Permite consultar el detalle de registros del modulo Registrar Combos.', 'RegistrarCombosApi', 'F_GetCombo', 'GET', 'REGISTRARCOMBOSAPI.F_GETCOMBO.GET', 1, now(), 'MigracionInicial'),
                (52, 'Metodo', 13, 'RegistrarCombos', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Registrar Combos.', 'RegistrarCombosApi', 'P_DeleteCombo', 'DELETE', 'REGISTRARCOMBOSAPI.P_DELETECOMBO.DELETE', 1, now(), 'MigracionInicial'),
                (53, 'Metodo', 13, 'RegistrarCombos', 'Crear', 'Permite registrar nueva informacion en el modulo Registrar Combos.', 'RegistrarCombosApi', 'P_InsCombo', 'POST', 'REGISTRARCOMBOSAPI.P_INSCOMBO.POST', 1, now(), 'MigracionInicial'),
                (54, 'Metodo', 13, 'RegistrarCombos', 'Actualizar', 'Permite modificar informacion existente del modulo Registrar Combos.', 'RegistrarCombosApi', 'P_UdpCombo', 'PUT', 'REGISTRARCOMBOSAPI.P_UDPCOMBO.PUT', 1, now(), 'MigracionInicial'),
                (55, 'Metodo', 13, 'RegistrarCombos', 'Cargar', 'Permite cargar archivos o imagenes del modulo Registrar Combos.', 'RegistrarCombosApi', 'P_UploadImagenCombo', 'POST', 'REGISTRARCOMBOSAPI.P_UPLOADIMAGENCOMBO.POST', 1, now(), 'MigracionInicial'),
                (56, 'Metodo', 12, 'RegistrarPrecios', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Registrar Precios.', 'RegistrarPreciosApi', 'P_DeletePrecioProducto', 'DELETE', 'REGISTRARPRECIOSAPI.P_DELETEPRECIOPRODUCTO.DELETE', 1, now(), 'MigracionInicial'),
                (57, 'Metodo', 12, 'RegistrarPrecios', 'Crear', 'Permite registrar nueva informacion en el modulo Registrar Precios.', 'RegistrarPreciosApi', 'P_InsPrecioProducto', 'POST', 'REGISTRARPRECIOSAPI.P_INSPRECIOPRODUCTO.POST', 1, now(), 'MigracionInicial'),
                (58, 'Metodo', 12, 'RegistrarPrecios', 'Actualizar', 'Permite modificar informacion existente del modulo Registrar Precios.', 'RegistrarPreciosApi', 'P_UdpPrecioProducto', 'PUT', 'REGISTRARPRECIOSAPI.P_UDPPRECIOPRODUCTO.PUT', 1, now(), 'MigracionInicial'),
                (59, 'Metodo', 10, 'RegistrarProductos', 'Consultar', 'Permite consultar el detalle de registros del modulo Registrar Productos.', 'RegistrarProductosApi', 'F_GetCuenta', 'GET', 'REGISTRARPRODUCTOSAPI.F_GETCUENTA.GET', 1, now(), 'MigracionInicial'),
                (60, 'Metodo', 10, 'RegistrarProductos', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Registrar Productos.', 'RegistrarProductosApi', 'P_DeleteCuenta', 'DELETE', 'REGISTRARPRODUCTOSAPI.P_DELETECUENTA.DELETE', 1, now(), 'MigracionInicial'),
                (61, 'Metodo', 10, 'RegistrarProductos', 'Crear', 'Permite registrar nueva informacion en el modulo Registrar Productos.', 'RegistrarProductosApi', 'P_InsCuenta', 'POST', 'REGISTRARPRODUCTOSAPI.P_INSCUENTA.POST', 1, now(), 'MigracionInicial'),
                (62, 'Metodo', 10, 'RegistrarProductos', 'Actualizar', 'Permite modificar informacion existente del modulo Registrar Productos.', 'RegistrarProductosApi', 'P_UdpCuenta', 'PUT', 'REGISTRARPRODUCTOSAPI.P_UDPCUENTA.PUT', 1, now(), 'MigracionInicial'),
                (63, 'Metodo', 9, 'RegistrarPublicaciones', 'Consultar', 'Permite consultar el detalle de registros del modulo Registrar Publicaciones.', 'RegistrarPublicacionesApi', 'F_GetInicioContenido', 'GET', 'REGISTRARPUBLICACIONESAPI.F_GETINICIOCONTENIDO.GET', 1, now(), 'MigracionInicial'),
                (64, 'Metodo', 9, 'RegistrarPublicaciones', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Registrar Publicaciones.', 'RegistrarPublicacionesApi', 'P_DeleteInicioContenido', 'DELETE', 'REGISTRARPUBLICACIONESAPI.P_DELETEINICIOCONTENIDO.DELETE', 1, now(), 'MigracionInicial'),
                (65, 'Metodo', 9, 'RegistrarPublicaciones', 'Crear', 'Permite registrar nueva informacion en el modulo Registrar Publicaciones.', 'RegistrarPublicacionesApi', 'P_InsInicioContenido', 'POST', 'REGISTRARPUBLICACIONESAPI.P_INSINICIOCONTENIDO.POST', 1, now(), 'MigracionInicial'),
                (66, 'Metodo', 9, 'RegistrarPublicaciones', 'Actualizar', 'Permite modificar informacion existente del modulo Registrar Publicaciones.', 'RegistrarPublicacionesApi', 'P_UdpInicioContenido', 'PUT', 'REGISTRARPUBLICACIONESAPI.P_UDPINICIOCONTENIDO.PUT', 1, now(), 'MigracionInicial'),
                (67, 'Metodo', 9, 'RegistrarPublicaciones', 'Cargar', 'Permite cargar archivos o imagenes del modulo Registrar Publicaciones.', 'RegistrarPublicacionesApi', 'P_UploadImagenInicio', 'POST', 'REGISTRARPUBLICACIONESAPI.P_UPLOADIMAGENINICIO.POST', 1, now(), 'MigracionInicial'),
                (68, 'Metodo', 3, 'Roles', 'Consultar', 'Permite consultar el detalle de registros del modulo Roles.', 'RolesApi', 'F_GetRol', 'GET', 'ROLESAPI.F_GETROL.GET', 1, now(), 'MigracionInicial'),
                (69, 'Metodo', 3, 'Roles', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Roles.', 'RolesApi', 'P_DeleteRol', 'DELETE', 'ROLESAPI.P_DELETEROL.DELETE', 1, now(), 'MigracionInicial'),
                (70, 'Metodo', 3, 'Roles', 'Crear', 'Permite registrar nueva informacion en el modulo Roles.', 'RolesApi', 'P_InsRol', 'POST', 'ROLESAPI.P_INSROL.POST', 1, now(), 'MigracionInicial'),
                (71, 'Metodo', 3, 'Roles', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Roles.', 'RolesApi', 'P_UdpRol', 'PUT', 'ROLESAPI.P_UDPROL.PUT', 1, now(), 'MigracionInicial'),
                (72, 'Metodo', 2, 'Usuarios', 'Asignar', 'Permite asignar o actualizar relaciones del modulo Usuarios.', 'RolesUserApi', 'Asignar', 'PUT', 'ROLESUSERAPI.ASIGNAR.PUT', 1, now(), 'MigracionInicial'),
                (73, 'Metodo', 2, 'Usuarios', 'Consultar', 'Permite consultar el detalle de registros del modulo Usuarios.', 'RolesUserApi', 'GetIdUserRoles', 'GET', 'ROLESUSERAPI.GETIDUSERROLES.GET', 1, now(), 'MigracionInicial'),
                (74, 'Metodo', 7, 'SistemaConfig', 'Consultar', 'Permite consultar el detalle de registros del modulo Sistema Config.', 'SistemaConfigApi', 'F_GetSistemaVisualConfig', 'GET', 'SISTEMACONFIGAPI.F_GETSISTEMAVISUALCONFIG.GET', 1, now(), 'MigracionInicial'),
                (75, 'Metodo', 7, 'SistemaConfig', 'Actualizar', 'Permite modificar informacion existente del modulo Sistema Config.', 'SistemaConfigApi', 'P_UdpSistemaVisualConfig', 'PUT', 'SISTEMACONFIGAPI.P_UDPSISTEMAVISUALCONFIG.PUT', 1, now(), 'MigracionInicial'),
                (76, 'Metodo', 7, 'SistemaConfig', 'Cargar', 'Permite cargar archivos o imagenes del modulo Sistema Config.', 'SistemaConfigApi', 'P_UploadImagenSistema', 'POST', 'SISTEMACONFIGAPI.P_UPLOADIMAGENSISTEMA.POST', 1, now(), 'MigracionInicial'),
                (77, 'Metodo', 7, 'SistemaConfig', 'Cargar', 'Permite cargar archivos o imagenes del modulo Sistema Config.', 'SistemaConfigApi', 'P_UploadVideoSistema', 'POST', 'SISTEMACONFIGAPI.P_UPLOADVIDEOSISTEMA.POST', 1, now(), 'MigracionInicial'),
                (78, 'Metodo', 8, 'Tiendas', 'Consultar', 'Permite consultar el detalle de registros del modulo Tiendas.', 'TiendaInternaApi', 'F_GetSaldoBilletera', 'GET', 'TIENDAINTERNAAPI.F_GETSALDOBILLETERA.GET', 1, now(), 'MigracionInicial'),
                (79, 'Metodo', 8, 'Tiendas', 'Crear', 'Permite registrar nueva informacion en el modulo Tiendas.', 'TiendaInternaApi', 'P_ConfirmarCompra', 'POST', 'TIENDAINTERNAAPI.P_CONFIRMARCOMPRA.POST', 1, now(), 'MigracionInicial'),
                (80, 'Metodo', 2, 'Usuarios', 'Consultar', 'Permite consultar el detalle de registros del modulo Usuarios.', 'UsuariosApi', 'F_GetUsuario', 'GET', 'USUARIOSAPI.F_GETUSUARIO.GET', 1, now(), 'MigracionInicial'),
                (81, 'Metodo', 2, 'Usuarios', 'Eliminar', 'Permite eliminar o inactivar registros del modulo Usuarios.', 'UsuariosApi', 'P_DeleteUsuario', 'DELETE', 'USUARIOSAPI.P_DELETEUSUARIO.DELETE', 1, now(), 'MigracionInicial'),
                (82, 'Metodo', 2, 'Usuarios', 'Actualizar', 'Permite modificar informacion existente del modulo Usuarios.', 'UsuariosApi', 'P_UdpUsuario', 'PUT', 'USUARIOSAPI.P_UDPUSUARIO.PUT', 1, now(), 'MigracionInicial');

                INSERT INTO "Roles_Permisos" ("Id_Rol", "Id_Permiso", "Vigente", "Fecha_Creacion", "Maquina_Creacion")
                SELECT 2, "Id_Permiso", 1, now(), 'MigracionInicial'
                FROM "Permisos"
                WHERE "TipoPermiso" = 'Menu'
                    AND "Id_Permiso" NOT IN (2, 3)
                UNION ALL
                SELECT 2, "Id_Permiso", 1, now(), 'MigracionInicial'
                FROM "Permisos"
                WHERE "TipoPermiso" = 'Metodo'
                    AND "Modulo" NOT IN ('Roles', 'Permisos')
                UNION ALL
                SELECT 3, "Id_Permiso", 1, now(), 'MigracionInicial'
                FROM "Permisos"
                WHERE "Id_Permiso" IN (6, 14, 30, 31, 78, 79);

                SELECT setval(pg_get_serial_sequence('"Roles"', 'Id_Rol'), COALESCE((SELECT MAX("Id_Rol") FROM "Roles"), 1), true);
                SELECT setval(pg_get_serial_sequence('"Dominios"', 'Id_Dominio'), COALESCE((SELECT MAX("Id_Dominio") FROM "Dominios"), 1), true);
                SELECT setval(pg_get_serial_sequence('"Menus"', 'Id_Menu'), COALESCE((SELECT MAX("Id_Menu") FROM "Menus"), 1), true);
                SELECT setval(pg_get_serial_sequence('"SistemaVisualConfig"', 'Id_SistemaVisualConfig'), COALESCE((SELECT MAX("Id_SistemaVisualConfig") FROM "SistemaVisualConfig"), 1), true);
                SELECT setval(pg_get_serial_sequence('"Permisos"', 'Id_Permiso'), COALESCE((SELECT MAX("Id_Permiso") FROM "Permisos"), 1), true);
                SELECT setval(pg_get_serial_sequence('"Roles_Permisos"', 'Id_Rol_Permiso'), COALESCE((SELECT MAX("Id_Rol_Permiso") FROM "Roles_Permisos"), 1), true);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "ComboPlataformas");

            migrationBuilder.DropTable(
                name: "CorreosPlataforma");

            migrationBuilder.DropTable(
                name: "ImagenesProducto");

            migrationBuilder.DropTable(
                name: "InicioContenidos");

            migrationBuilder.DropTable(
                name: "MovimientosBilletera");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PedidoCuentas");

            migrationBuilder.DropTable(
                name: "PreciosProducto");

            migrationBuilder.DropTable(
                name: "Roles_Permisos");

            migrationBuilder.DropTable(
                name: "Roles_User");

            migrationBuilder.DropTable(
                name: "SistemaVisualConfig");

            migrationBuilder.DropTable(
                name: "BilleteraVendedores");

            migrationBuilder.DropTable(
                name: "Cuentas");

            migrationBuilder.DropTable(
                name: "PedidoDetalles");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "CodigosCompra");

            migrationBuilder.DropTable(
                name: "Dominios");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}

