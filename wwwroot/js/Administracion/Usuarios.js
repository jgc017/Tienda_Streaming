// Obtiene el token antifalsificacion generado en VwUsuarios.cshtml.
// Lo consumen las peticiones POST, PUT y DELETE del CRUD.
let tablaUsuarios;

function getCsrfToken() {
    return document.getElementById("csrfToken")?.value || "";
}

// Wrapper de fetch usado por todo este archivo.
// Agrega credenciales de la cookie y el header X-CSRF-TOKEN cuando el metodo
// modifica datos.
function secureFetch(url, options = {}) {
    const method = (options.method || "GET").toUpperCase();
    const headers = new Headers(options.headers || {});

    if (method !== "GET" && method !== "HEAD" && method !== "OPTIONS") {
        headers.set("X-CSRF-TOKEN", getCsrfToken());
    }

    return fetch(url, {
        ...options,
        headers,
        credentials: "same-origin"
    });
}

// Normaliza respuestas del API:
// - 401 redirige al login.
// - 403 muestra acceso denegado.
// - Otros errores intentan leer el JSON { ok, mensaje } devuelto por el backend.
async function parseJsonResponse(response) {
    if (response.status === 401) {
        window.location.href = "/Account/Login";
        return null;
    }

    if (response.status === 403) {
        mostrarAlerta("advertencia", "Acceso denegado", "No tienes permisos para realizar esta accion.");
        return null;
    }

    const data = await response.json().catch(() => null);
    if (!response.ok && !data) {
        mostrarAlerta("error", "Error", "No se pudo procesar la solicitud.");
        return null;
    }

    return data;
}

// Inicializacion de la vista: arma la tabla, consulta usuarios y conecta botones.
document.addEventListener("DOMContentLoaded", () => {
    F_GetUsuariosList();

    document.getElementById("BtnRegistrar")?.addEventListener("click", P_InsUsuario);

    document.querySelector("#modalEditar .btn-close-custom")?.addEventListener("click", cerrarModal);
    document.getElementById("btnCancelarModal")?.addEventListener("click", cerrarModal);
    document.getElementById("btnGuardarCambios")?.addEventListener("click", P_UdpUsuario);

    document.querySelector("#modalAsigRoles .btn-close-custom")?.addEventListener("click", cerrarModalRoles);
    document.getElementById("btnCancelarModalRol")?.addEventListener("click", cerrarModalRoles);
    document.getElementById("btnGuardarCambiosRol")?.addEventListener("click", P_UdpUsuarioRoles);

    document.getElementById("btnCerrarCredencialesUsuario")?.addEventListener("click", cerrarModalCredencialesUsuario);
    document.getElementById("btnCerrarCredencialesUsuarioFooter")?.addEventListener("click", cerrarModalCredencialesUsuario);
    document.getElementById("btnCopiarCredencialesUsuario")?.addEventListener("click", copiarCredencialesUsuario);
});

// P_InsUsuario: valida el formulario superior y registra un nuevo usuario.
function P_InsUsuario() {
    limpiarErrores();

    let valido = true;

    const nombre = txtNombreCompleto.value.trim();
    const usuario = txtUsuario.value.trim();
    const email = txtEmail.value.trim();
    const pass = txtPassword.value.trim();
    const pass2 = txtPassWordConfir.value.trim();

    if (nombre === "") { mostrarError("txtNombreCompleto", "El nombre es obligatorio"); valido = false; }
    if (usuario === "") { mostrarError("txtUsuario", "El usuario es obligatorio"); valido = false; }
    if (email === "") { mostrarError("txtEmail", "El email es obligatorio"); valido = false; }
    if (pass !== "" && pass.length < 10) { mostrarError("txtPassword", "La contrasena debe tener minimo 10 caracteres"); valido = false; }
    if (pass !== "" && !/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(pass)) {
        mostrarError("txtPassword", "Debe incluir mayuscula, minuscula y numero");
        valido = false;
    }
    if (pass !== "" && pass2 === "") { mostrarError("txtPassWordConfir", "Debe confirmar la contrasena temporal"); valido = false; }
    if (pass === "" && pass2 !== "") { mostrarError("txtPassword", "Escribe la contrasena temporal o deja ambos campos vacios"); valido = false; }
    if (pass !== "" && pass2 !== "" && pass !== pass2) {
        mostrarError("txtPassWordConfir", "Las contrasenas no coinciden");
        valido = false;
    }

    if (!valido) return;

    mostrarConfirmacion(
        "Registrar usuario?",
        "Verifica que los datos sean correctos.",
        (confirmado) => {
            if (!confirmado) return;

            const nuevoUsuario = {
                nombre: nombre,
                usuario: usuario,
                e_Mail: email,
                password: pass || null
            };

            secureFetch("/api/UsuariosApi/P_InsUsuario", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(nuevoUsuario)
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarCredencialesUsuario(data.data?.credenciales, data.mensaje);
                        limpiarFormularioRegistro();
                        F_GetUsuariosList();
                    } else {
                        mostrarAlerta("error", "Error al registrar", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo procesar la solicitud.");
                });
        }
    );
}

// Limpia el formulario superior despues de una creacion exitosa.
function limpiarFormularioRegistro() {
    txtNombreCompleto.value = "";
    txtUsuario.value = "";
    txtEmail.value = "";
    txtPassword.value = "";
    txtPassWordConfir.value = "";
}

// F_GetUsuariosList: arma la grilla de usuarios y consulta los datos que la alimentan.
function F_GetUsuariosList() {
    if (!tablaUsuarios) {
        tablaUsuarios = Grilla({
            tableSelector: "#tablaUsuarios",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "nombre",
            columns: [
                { title: "Nombre", data: "nombre", width: 30 },
                { title: "Usuario", data: "usuario", width: 20 },
                { title: "Email", data: "e_Mail", width: 30 },
                {
                    title: "Estado",
                    data: "vigente",
                    width: 10,
                    className: "text-center",
                    searchable: false,
                    render: renderEstadoUsuario
                }
            ],
            actions: {
                title: "Opciones",
                width: 10,
                items: [
                    {
                        action: "consultar",
                        label: "Consultar",
                        icon: "fa-solid fa-eye",
                        onClick: consultarUsuario
                    },
                    {
                        action: "actualizar",
                        label: "Actualizar",
                        icon: "fa-solid fa-pen-to-square",
                        onClick: row => F_GetUsuario(row.id_Usuario)
                    },
                    {
                        action: "asignarRol",
                        label: "Asignar Rol",
                        icon: "fa-solid fa-user-check",
                        onClick: row => F_GetUsuarioRoles(row.id_Usuario)
                    },
                    {
                        action: "eliminar",
                        label: "Eliminar",
                        icon: "fa-solid fa-trash",
                        onClick: row => P_DeleteUsuario(row.id_Usuario)
                    }
                ]
            }
        });
    }

    secureFetch("/api/UsuariosApi/F_GetUsuariosList")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            tablaUsuarios.setData(data.data);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de usuarios.");
        });
}

// Crea la etiqueta visual del estado del usuario.
function renderEstadoUsuario(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

// Muestra los datos principales del usuario sin abrir el modal de actualizacion.
function consultarUsuario(u) {
    mostrarAlerta(
        "info",
        "Detalle del usuario",
        `Nombre: ${u.nombre || ""}\nUsuario: ${u.usuario || ""}\nEmail: ${u.e_Mail || ""}`
    );
}

// F_GetUsuario: consulta un usuario por id y carga el modal de actualizacion.
function F_GetUsuario(Id_Usuario) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/UsuariosApi/F_GetUsuario/${Id_Usuario}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const u = data.data;

            document.getElementById("Id_Usuario").value = u.id_Usuario;
            document.getElementById("Nombre").value = u.nombre;
            document.getElementById("Usuario").value = u.usuario;
            document.getElementById("E_Mail").value = u.e_Mail;

            abrirModal();

            // Re-crea el toggle cada vez que abre el modal para reflejar el estado vigente.
            setTimeout(() => {
                $('#chkVigente').bootstrapToggle('destroy');
                $('#chkVigente').bootstrapToggle({
                    on: 'Activo',
                    off: 'Inactivo',
                    onstyle: 'primary',
                    offstyle: 'danger',
                    size: 'small',
                    width: 92,
                    height: 30
                });
                $('#chkVigente').bootstrapToggle(u.vigente === 1 ? 'on' : 'off');
            }, 50);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el usuario.");
        });
}

// F_GetUsuarioRoles: consulta usuario y roles disponibles para abrir el modal de asignacion.
function F_GetUsuarioRoles(Id_Usuario) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/RolesUserApi/GetIdUserRoles/${Id_Usuario}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const u = data.data;

            document.getElementById("Id_Usuario_Rol").value = u.id_Usuario;
            document.getElementById("NombreRoles").value = u.nombre;
            document.getElementById("E_Mail_Roles").value = u.e_Mail || "";
            renderRolesAsignables(u.roles || []);

            abrirModalRoles();
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la asignacion de roles.");
        });
}

// Pinta la lista de roles activos y marca los que el usuario ya tiene asignados.
function renderRolesAsignables(roles) {
    const contenedor = document.getElementById("rolesAsignables");
    contenedor.textContent = "";

    if (!roles.length) {
        const empty = document.createElement("div");
        empty.className = "text-secondary py-2";
        empty.textContent = "No hay roles activos disponibles.";
        contenedor.appendChild(empty);
        return;
    }

    roles.forEach(rol => {
        const item = document.createElement("label");
        item.className = "role-assignment-item";

        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.className = "form-check-input";
        checkbox.value = rol.id_Rol;
        checkbox.checked = Boolean(rol.asignado);

        const texto = document.createElement("span");
        texto.textContent = rol.rol;

        item.appendChild(checkbox);
        item.appendChild(texto);
        contenedor.appendChild(item);
    });
}

// Muestra el modal de actualizacion de usuario.
function abrirModal() {
    document.getElementById("modalEditar").style.display = "flex";
}

// Oculta el modal de actualizacion de usuario.
function cerrarModal() {
    document.getElementById("modalEditar").style.display = "none";
}

// Muestra el modal para asignar roles al usuario.
function abrirModalRoles() {
    document.getElementById("modalAsigRoles").style.display = "flex";
}

// Oculta el modal para asignar roles al usuario.
function cerrarModalRoles() {
    document.getElementById("modalAsigRoles").style.display = "none";
}

// P_UdpUsuarioRoles: sincroniza los roles seleccionados para el usuario.
function P_UdpUsuarioRoles() {
    const idUsuario = document.getElementById("Id_Usuario_Rol").value;
    const rolesSeleccionados = Array.from(
        document.querySelectorAll("#rolesAsignables input[type='checkbox']:checked")
    ).map(input => Number(input.value));

    mostrarConfirmacion(
        "Guardar roles?",
        "Los roles seleccionados quedaran activos para este usuario.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/RolesUserApi/asignar/${idUsuario}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ roleIds: rolesSeleccionados })
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        cerrarModalRoles();
                        mostrarAlerta("exito", "Roles actualizados", data.mensaje);
                        F_GetUsuariosList();
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar la asignacion de roles.");
                });
        }
    );
}

// P_UdpUsuario: toma los datos del modal y actualiza el usuario seleccionado.
function P_UdpUsuario() {
    const Id_Usuario = document.getElementById("Id_Usuario").value;
    const nombre = document.getElementById("Nombre").value.trim();
    const usuario = document.getElementById("Usuario").value.trim();
    const email = document.getElementById("E_Mail").value.trim();

    if (!nombre || !usuario || !email) {
        mostrarAlerta("advertencia", "Datos incompletos", "Nombre, usuario y email son obligatorios.");
        return;
    }

    const usuarioActualizado = {
        nombre: nombre,
        usuario: usuario,
        e_Mail: email,
        vigente: document.getElementById("chkVigente").checked ? 1 : 0
    };

    secureFetch(`/api/UsuariosApi/P_UdpUsuario/${Id_Usuario}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(usuarioActualizado)
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                cerrarModal();
                mostrarAlerta("exito", "Actualizado", data.mensaje);
                F_GetUsuariosList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el usuario.");
        });
}

// P_DeleteUsuario: confirma y ejecuta la baja logica de un usuario.
function P_DeleteUsuario(Id_Usuario) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar usuario?",
        "Esta accion marcara el usuario como inactivo.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/UsuariosApi/P_DeleteUsuario/${Id_Usuario}`, {
                method: "DELETE"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Usuario eliminado", data.mensaje);
                        F_GetUsuariosList();
                    } else {
                        mostrarAlerta("error", "Error al eliminar", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo procesar la solicitud.");
                });
        }
    );
}

function mostrarCredencialesUsuario(credenciales, mensaje) {
    if (!credenciales) {
        mostrarAlerta("exito", "Usuario registrado", mensaje || "Usuario registrado correctamente.");
        return;
    }

    const texto = formatearCredencialesUsuario(credenciales);
    document.getElementById("txtCredencialesUsuario").value = texto;
    document.getElementById("modalCredencialesUsuario").style.display = "flex";
}

function cerrarModalCredencialesUsuario() {
    document.getElementById("modalCredencialesUsuario").style.display = "none";
    document.getElementById("txtCredencialesUsuario").value = "";
}

function copiarCredencialesUsuario() {
    const texto = document.getElementById("txtCredencialesUsuario").value;

    if (navigator.clipboard?.writeText) {
        navigator.clipboard.writeText(texto)
            .then(() => mostrarAlerta("exito", "Copiado", "Datos copiados correctamente."))
            .catch(copiarCredencialesUsuarioFallback);
        return;
    }

    copiarCredencialesUsuarioFallback();
}

function copiarCredencialesUsuarioFallback() {
    const textarea = document.getElementById("txtCredencialesUsuario");
    textarea.focus();
    textarea.select();
    document.execCommand("copy");
    mostrarAlerta("exito", "Copiado", "Datos copiados correctamente.");
}

function formatearCredencialesUsuario(credenciales) {
    const plataforma = obtenerValorUsuario(credenciales, "plataforma", "Plataforma") || "Tienda Streaming";
    const tipoUsuario = obtenerValorUsuario(credenciales, "tipoUsuario", "TipoUsuario", "tipo_Usuario", "Tipo_Usuario") || "Usuario del sistema";
    const usuario = obtenerValorUsuario(credenciales, "usuario", "Usuario") || "";
    const contrasena = obtenerValorUsuario(credenciales, "contrasena", "Contrasena", "contraseña", "Contraseña") || "";
    const linkAcceso = obtenerValorUsuario(credenciales, "linkAcceso", "LinkAcceso", "link_Acceso", "Link_Acceso") || "";

    return `Le ha sido creado el siguiente usuario en la plataforma
**Plataforma:** ${plataforma}
**Tipo Usuario:** ${tipoUsuario}
**usuario:** ${usuario}
**Contraseña:** ${contrasena}
Para acceder puede hacerlo a traves de la siguiente URL, una vez ingrese debera actualizar su contraseña:
**Link Acceso:** ${linkAcceso}`;
}

function obtenerValorUsuario(item, ...keys) {
    if (!item) return "";
    for (const key of keys) {
        if (Object.prototype.hasOwnProperty.call(item, key) && item[key] !== null && item[key] !== undefined) {
            return item[key];
        }
    }
    return "";
}
