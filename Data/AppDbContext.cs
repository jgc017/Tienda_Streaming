using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Models.Account;
using Tienda_Streaming.Models.Administracion;
using System.Reflection.Emit;

namespace Tienda_Streaming.Data
{
    // DbContext principal de Entity Framework Core.
    // Centraliza las tablas del sistema y las reglas de mapeo que luego usan
    // los controladores AccountController y UsuariosApiController.
    public class AppDbContext : DbContext
    {
        // Recibe opciones configuradas en Program.cs, incluida la cadena Npgsql.
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Tablas de administracion y seguridad.
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Auditoria> Auditoria { get; set; }
        public DbSet<Dominios> Dominios { get; set; }
        public DbSet<Menus> Menus { get; set; }
        public DbSet<InicioContenido> InicioContenidos { get; set; }
        public DbSet<SistemaVisualConfig> SistemaVisualConfig { get; set; }
        public DbSet<Cuentas> Cuentas { get; set; }
        public DbSet<PreciosProducto> PreciosProducto { get; set; }
        public DbSet<Combos> Combos { get; set; }
        public DbSet<ComboPlataformas> ComboPlataformas { get; set; }
        public DbSet<BilleteraVendedores> BilleteraVendedores { get; set; }
        public DbSet<MovimientosBilletera> MovimientosBilletera { get; set; }
        public DbSet<CodigosCompra> CodigosCompra { get; set; }
        public DbSet<CorreosPlataforma> CorreosPlataforma { get; set; }
        public DbSet<Pedidos> Pedidos { get; set; }
        public DbSet<PedidoDetalles> PedidoDetalles { get; set; }
        public DbSet<PedidoCuentas> PedidoCuentas { get; set; }
        public DbSet<ImagenesProducto> ImagenesProducto { get; set; }
        public DbSet<Permisos> Permisos { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Roles_Permisos> Roles_Permisos { get; set; }
        public DbSet<Roles_User> Roles_User { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        // Configura indices, tamanos, obligatoriedad y relaciones.
        // Estas reglas se convierten en migraciones y constraints de base de datos.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuarios: entidad central para login, CRUD y recuperacion.
            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.HasIndex(u => u.Usuario).IsUnique();
                entity.HasIndex(u => u.E_Mail).IsUnique();
                entity.Property(u => u.Usuario).HasMaxLength(60).IsRequired();
                entity.Property(u => u.E_Mail).HasMaxLength(160);
                entity.Property(u => u.Nombre).HasMaxLength(120).IsRequired();
                entity.Property(u => u.Password).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Debe_Cambiar_Password).HasDefaultValue((short)0);
            });

            // Tokens de recuperacion: se busca por hash y expiran por fecha.
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity.HasIndex(t => new { t.Id_Usuario, t.Fecha_Expiracion });
                entity.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
                entity.Property(t => t.Ip_Solicitud).HasMaxLength(80);
                entity.HasOne<Usuarios>()
                    .WithMany()
                    .HasForeignKey(t => t.Id_Usuario)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Roles del sistema. Rol debe ser unico para evitar duplicados.
            modelBuilder.Entity<Roles>(entity =>
            {
                entity.HasIndex(r => r.Rol).IsUnique();
                entity.Property(r => r.Rol).HasMaxLength(80).IsRequired();
            });

            // Permisos por modulo/accion. El indice evita repetir la misma accion.
            modelBuilder.Entity<Permisos>(entity =>
            {
                entity.HasIndex(p => new { p.TipoPermiso, p.Id_Menu, p.Accion })
                    .IsUnique()
                    .HasFilter("\"Id_Menu\" IS NOT NULL AND \"TipoPermiso\" = 'Menu'");
                entity.HasIndex(p => p.CodigoPermiso)
                    .IsUnique()
                    .HasFilter("\"CodigoPermiso\" IS NOT NULL");
                entity.Property(p => p.TipoPermiso).HasMaxLength(20).HasDefaultValue("Menu").IsRequired();
                entity.Property(p => p.Modulo).HasMaxLength(80).IsRequired();
                entity.Property(p => p.Accion).HasMaxLength(80).IsRequired();
                entity.Property(p => p.Descripcion).HasMaxLength(200);
                entity.Property(p => p.Controlador).HasMaxLength(120);
                entity.Property(p => p.Metodo).HasMaxLength(120);
                entity.Property(p => p.HttpMetodo).HasMaxLength(20);
                entity.Property(p => p.CodigoPermiso).HasMaxLength(300);
                entity.HasOne(p => p.Menu)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Menu)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Dominios o catalogos jerarquicos. Id_Padre apunta a otro dominio.
            modelBuilder.Entity<Dominios>(entity =>
            {
                entity.Property(d => d.Descripcion).HasMaxLength(120).IsRequired();
                entity.Property(d => d.DominioPadre).HasMaxLength(2).HasDefaultValue("No").IsRequired();
                entity.HasOne(d => d.Padre)
                    .WithMany()
                    .HasForeignKey(d => d.Id_Padre)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Menus: estructura jerarquica del menu principal de la aplicacion.
            modelBuilder.Entity<Menus>(entity =>
            {
                entity.Property(m => m.Descripcion).HasMaxLength(255).IsRequired();
                entity.Property(m => m.Tipo).HasMaxLength(255);
                entity.Property(m => m.Controlador).HasMaxLength(255);
                entity.Property(m => m.Vista).HasMaxLength(255);
                entity.Property(m => m.Icono).HasMaxLength(255);
                entity.HasIndex(m => new { m.Controlador, m.Vista });
                entity.HasOne(m => m.Padre)
                    .WithMany()
                    .HasForeignKey(m => m.Id_Padre)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // InicioContenidos: contenido administrable para la pagina publica.
            modelBuilder.Entity<InicioContenido>(entity =>
            {
                entity.HasIndex(i => new { i.TipoContenido, i.Orden });
                entity.Property(i => i.TipoContenido).HasMaxLength(40).IsRequired();
                entity.Property(i => i.Titulo).HasMaxLength(160).IsRequired();
                entity.Property(i => i.Resumen).HasMaxLength(500);
                entity.Property(i => i.ImagenUrl).HasMaxLength(500);
                entity.Property(i => i.EnlaceUrl).HasMaxLength(500);
                entity.Property(i => i.TextoBoton).HasMaxLength(80);
            });

            // SistemaVisualConfig: imagenes globales usadas por layouts y loader.
            modelBuilder.Entity<SistemaVisualConfig>(entity =>
            {
                entity.Property(s => s.LogoUrl).HasMaxLength(500).IsRequired();
                entity.Property(s => s.FaviconUrl).HasMaxLength(500).IsRequired();
                entity.Property(s => s.LoginBackgroundUrl).HasMaxLength(500).IsRequired();
                entity.Property(s => s.VideoUrl).HasMaxLength(500);
            });

            // Cuentas: inventario de cuentas disponibles por plataforma y tipo de usuario.
            modelBuilder.Entity<Cuentas>(entity =>
            {
                entity.HasIndex(c => new { c.Id_Plataforma, c.Id_Tipo_Usuario, c.Vigente });
                entity.Property(c => c.Tiempo_Pantalla).HasDefaultValue(30);
                entity.Property(c => c.Correo_Cuenta).HasMaxLength(160).IsRequired();
                entity.Property(c => c.Contrasena_Cuenta)
                    .HasColumnName("Contraseña_Cuenta")
                    .HasMaxLength(1000)
                    .IsRequired();
                entity.Property(c => c.Perfil_Cuenta).HasMaxLength(80);
                entity.Property(c => c.Pin_Cuenta).HasMaxLength(20);
                entity.HasOne(c => c.Plataforma)
                    .WithMany()
                    .HasForeignKey(c => c.Id_Plataforma)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.TipoUsuario)
                    .WithMany()
                    .HasForeignKey(c => c.Id_Tipo_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // PreciosProducto: catalogo comercial separado del inventario de cuentas.
            modelBuilder.Entity<PreciosProducto>(entity =>
            {
                entity.HasIndex(p => new { p.Id_Plataforma, p.Id_Tipo_Usuario, p.Tiempo_Pantalla }).IsUnique();
                entity.Property(p => p.Precio).HasColumnType("numeric(12,2)");
                entity.HasOne(p => p.Plataforma)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Plataforma)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.TipoUsuario)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Tipo_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Combos: producto comercial compuesto por plataformas existentes.
            modelBuilder.Entity<Combos>(entity =>
            {
                entity.HasIndex(c => new { c.Nombre, c.Id_Tipo_Usuario, c.Tiempo_Pantalla }).IsUnique();
                entity.Property(c => c.Nombre).HasMaxLength(120).IsRequired();
                entity.Property(c => c.Descripcion).HasMaxLength(500);
                entity.Property(c => c.ImagenUrl).HasMaxLength(500);
                entity.Property(c => c.Precio).HasColumnType("numeric(12,2)");
                entity.Property(c => c.Orden).HasDefaultValue(1);
                entity.HasOne(c => c.TipoUsuario)
                    .WithMany()
                    .HasForeignKey(c => c.Id_Tipo_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ComboPlataformas>(entity =>
            {
                entity.HasIndex(c => new { c.Id_Combo, c.Id_Plataforma }).IsUnique();
                entity.Property(c => c.Cantidad).HasDefaultValue(1);
                entity.HasOne(c => c.Combo)
                    .WithMany(c => c.Plataformas)
                    .HasForeignKey(c => c.Id_Combo)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Plataforma)
                    .WithMany()
                    .HasForeignKey(c => c.Id_Plataforma)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BilleteraVendedores>(entity =>
            {
                entity.HasIndex(b => b.Id_Usuario).IsUnique();
                entity.Property(b => b.Saldo).HasColumnType("numeric(12,2)");
                entity.HasOne(b => b.Usuario)
                    .WithMany()
                    .HasForeignKey(b => b.Id_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MovimientosBilletera>(entity =>
            {
                entity.Property(m => m.Tipo_Movimiento).HasMaxLength(20).IsRequired();
                entity.Property(m => m.Valor).HasColumnType("numeric(12,2)");
                entity.Property(m => m.Saldo_Anterior).HasColumnType("numeric(12,2)");
                entity.Property(m => m.Saldo_Nuevo).HasColumnType("numeric(12,2)");
                entity.Property(m => m.Descripcion).HasMaxLength(200);
                entity.HasOne(m => m.Billetera)
                    .WithMany()
                    .HasForeignKey(m => m.Id_Billetera)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Pedido)
                    .WithMany()
                    .HasForeignKey(m => m.Id_Pedido)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CodigosCompra>(entity =>
            {
                entity.HasIndex(c => c.Codigo).IsUnique();
                entity.Property(c => c.Codigo).HasMaxLength(40).IsRequired();
                entity.Property(c => c.Nombre_Cliente).HasMaxLength(120).IsRequired();
                entity.Property(c => c.Correo_Cliente).HasMaxLength(160).IsRequired();
                entity.Property(c => c.Valor_Inicial).HasColumnType("numeric(12,2)");
                entity.Property(c => c.Saldo_Disponible).HasColumnType("numeric(12,2)");
            });

            modelBuilder.Entity<CorreosPlataforma>(entity =>
            {
                entity.HasIndex(c => c.Hash_Mensaje).IsUnique();
                entity.HasIndex(c => c.Fecha_Recepcion);
                entity.Property(c => c.MessageId).HasMaxLength(160);
                entity.Property(c => c.Hash_Mensaje).HasMaxLength(128).IsRequired();
                entity.Property(c => c.Remitente).HasMaxLength(300);
                entity.Property(c => c.Destinatarios).HasMaxLength(1000);
                entity.Property(c => c.Asunto).HasMaxLength(300);
                entity.Property(c => c.Encabezados).HasColumnType("text");
                entity.Property(c => c.Cuerpo_Texto).HasColumnType("text");
                entity.Property(c => c.Cuerpo_Html).HasColumnType("text");
                entity.Property(c => c.Texto_Busqueda).HasColumnType("text").IsRequired();
            });

            modelBuilder.Entity<Pedidos>(entity =>
            {
                entity.Property(p => p.Origen).HasMaxLength(20).IsRequired();
                entity.Property(p => p.Nombre_Cliente).HasMaxLength(120).IsRequired();
                entity.Property(p => p.Correo_Cliente).HasMaxLength(160);
                entity.Property(p => p.Total).HasColumnType("numeric(12,2)");
                entity.HasOne(p => p.TipoUsuario)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Tipo_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Usuario)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.CodigoCompra)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Codigo_Compra)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PedidoDetalles>(entity =>
            {
                entity.Property(d => d.Tipo_Producto).HasMaxLength(20).IsRequired();
                entity.Property(d => d.Precio_Unitario).HasColumnType("numeric(12,2)");
                entity.Property(d => d.Subtotal).HasColumnType("numeric(12,2)");
                entity.HasOne(d => d.Pedido)
                    .WithMany(p => p.Detalles)
                    .HasForeignKey(d => d.Id_Pedido)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(d => d.Plataforma)
                    .WithMany()
                    .HasForeignKey(d => d.Id_Plataforma)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.Combo)
                    .WithMany()
                    .HasForeignKey(d => d.Id_Combo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PedidoCuentas>(entity =>
            {
                entity.HasIndex(p => new { p.Id_Pedido, p.Id_Cuenta }).IsUnique();
                entity.HasOne(p => p.Pedido)
                    .WithMany(p => p.Cuentas)
                    .HasForeignKey(p => p.Id_Pedido)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Cuenta)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Cuenta)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.PedidoDetalle)
                    .WithMany()
                    .HasForeignKey(p => p.Id_Pedido_Detalle)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ImagenesProducto: imagen visible por plataforma en las tiendas.
            modelBuilder.Entity<ImagenesProducto>(entity =>
            {
                entity.HasIndex(i => new { i.Id_Plataforma, i.Id_Tipo_Imagen }).IsUnique();
                entity.HasIndex(i => i.Orden);
                entity.Property(i => i.Orden).HasDefaultValue(1);
                entity.Property(i => i.ImagenUrl).HasMaxLength(500).IsRequired();
                entity.Property(i => i.Descripcion).HasMaxLength(200);
                entity.HasOne(i => i.Plataforma)
                    .WithMany()
                    .HasForeignKey(i => i.Id_Plataforma)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.TipoImagen)
                    .WithMany()
                    .HasForeignKey(i => i.Id_Tipo_Imagen)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Tabla puente entre roles y permisos.
            modelBuilder.Entity<Roles_Permisos>(entity =>
            {
                entity.HasIndex(rp => new { rp.Id_Rol, rp.Id_Permiso }).IsUnique();
                entity.HasOne(rp => rp.Rol)
                    .WithMany()
                    .HasForeignKey(rp => rp.Id_Rol)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rp => rp.Permiso)
                    .WithMany()
                    .HasForeignKey(rp => rp.Id_Permiso)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Tabla puente entre usuarios y roles.
            modelBuilder.Entity<Roles_User>(entity =>
            {
                entity.HasIndex(ru => new { ru.Id_Usuario, ru.Id_Rol }).IsUnique();
                entity.HasOne(ru => ru.Usuario)
                    .WithMany()
                    .HasForeignKey(ru => ru.Id_Usuario)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ru => ru.Rol)
                    .WithMany()
                    .HasForeignKey(ru => ru.Id_Rol)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Auditoria: guarda usuario creador/modificador cuando exista.
            modelBuilder.Entity<Auditoria>(entity =>
            {
                entity.Property(a => a.Descripcion).HasMaxLength(500);
                entity.Property(a => a.Formulario).HasMaxLength(120);
                entity.Property(a => a.Metodo_Ejecutado).HasMaxLength(120);
                entity.HasOne(a => a.UsuarioCrea)
                    .WithMany()
                    .HasForeignKey(a => a.Id_Usuario_Creacion)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.UsuarioModifica)
                    .WithMany()
                    .HasForeignKey(a => a.Id_Usuario_Modifica)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
