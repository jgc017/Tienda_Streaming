// Obtiene el token antifalsificacion generado en Roles.cshtml.
// Lo consumen las peticiones POST, PUT y DELETE del CRUD.
let tablaRoles;

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

// Inicializacion de la vista: carga roles y conecta botones del formulario/modal.
document.addEventListener("DOMContentLoaded", () => {
    F_GetRolesList();

    document.getElementById("BtnRegistrarRol")?.addEventListener("click", P_InsRol);
    document.querySelector("#modalEditarRol .btn-close-custom")?.addEventListener("click", cerrarModalRol);
    document.getElementById("btnCancelarRolModal")?.addEventListener("click", cerrarModalRol);
    document.getElementById("btnGuardarRolCambios")?.addEventListener("click", P_UdpRol);
});

// P_InsRol: valida el formulario superior y registra un nuevo rol.
function P_InsRol() {
    limpiarErrores();

    const rol = document.getElementById("txtRol").value.trim();

    if (!rol) {
        mostrarError("txtRol", "El rol es obligatorio");
        return;
    }

    if (rol.length < 2) {
        mostrarError("txtRol", "El rol debe tener minimo 2 caracteres");
        return;
    }

    mostrarConfirmacion(
        "Registrar rol?",
        "Verifica que el nombre del rol sea correcto.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch("/api/RolesApi/P_InsRol", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ rol })
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Rol registrado", data.mensaje);
                        document.getElementById("txtRol").value = "";
                        F_GetRolesList();
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

// F_GetRolesList: arma la grilla de roles y consulta los datos que la alimentan.
function F_GetRolesList() {
    if (!tablaRoles) {
        tablaRoles = Grilla({
            tableSelector: "#tablaRoles",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "rol",
            columns: [
                { title: "Rol", data: "rol", width: 35 },
                {
                    title: "Estado",
                    data: "vigente",
                    width: 15,
                    className: "text-center",
                    searchable: false,
                    render: renderEstadoRol
                },
                {
                    title: "Fecha creacion",
                    data: "fecha_Creacion",
                    width: 35,
                    searchable: false,
                    render: formatearFecha
                }
            ],
            actions: {
                title: "Opciones",
                width: 15,
                items: [
                    {
                        action: "consultar",
                        label: "Consultar",
                        icon: "fa-solid fa-eye",
                        onClick: consultarRol
                    },
                    {
                        action: "actualizar",
                        label: "Actualizar",
                        icon: "fa-solid fa-pen-to-square",
                        onClick: row => F_GetRol(row.id_Rol)
                    },
                    {
                        action: "eliminar",
                        label: "Eliminar",
                        icon: "fa-solid fa-trash",
                        onClick: row => P_DeleteRol(row.id_Rol)
                    }
                ]
            }
        });
    }

    secureFetch("/api/RolesApi/F_GetRolesList")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            tablaRoles.setData(data.data);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de roles.");
        });
}

// Formatea fechas ISO devueltas por el API para lectura en tabla.
function formatearFecha(fecha) {
    if (!fecha) return "";
    return new Date(fecha).toLocaleString();
}

// Crea la etiqueta visual del estado del rol.
function renderEstadoRol(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

// Muestra los datos principales del rol sin abrir el modal de edicion.
function consultarRol(rol) {
    mostrarAlerta(
        "info",
        "Detalle del rol",
        `Rol: ${rol.rol || ""}\nEstado: ${rol.vigente == 1 ? "Activo" : "Inactivo"}\nFecha creacion: ${formatearFecha(rol.fecha_Creacion)}`
    );
}

// F_GetRol: consulta un rol por id y carga el modal de actualizacion.
function F_GetRol(idRol) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/RolesApi/F_GetRol/${idRol}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const rol = data.data;

            document.getElementById("Id_Rol").value = rol.id_Rol;
            document.getElementById("Rol").value = rol.rol;

            abrirModalRol();

            // Re-crea el toggle cada vez que abre el modal para reflejar el estado vigente.
            setTimeout(() => {
                $('#chkRolVigente').bootstrapToggle('destroy');
                $('#chkRolVigente').bootstrapToggle({
                    on: 'Activo',
                    off: 'Inactivo',
                    onstyle: 'primary',
                    offstyle: 'danger',
                    size: 'small',
                    width: 92,
                    height: 30
                });
                $('#chkRolVigente').bootstrapToggle(rol.vigente === 1 ? 'on' : 'off');
            }, 50);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el rol.");
        });
}

// Muestra el modal de edicion.
function abrirModalRol() {
    document.getElementById("modalEditarRol").style.display = "flex";
}

// Oculta el modal de edicion.
function cerrarModalRol() {
    document.getElementById("modalEditarRol").style.display = "none";
}

// P_UdpRol: toma los datos del modal y actualiza el rol seleccionado.
function P_UdpRol() {
    const idRol = document.getElementById("Id_Rol").value;
    const rol = document.getElementById("Rol").value.trim();

    if (!rol) {
        mostrarAlerta("advertencia", "Datos incompletos", "El nombre del rol es obligatorio.");
        return;
    }

    const rolActualizado = {
        rol: rol,
        vigente: document.getElementById("chkRolVigente").checked ? 1 : 0
    };

    secureFetch(`/api/RolesApi/P_UdpRol/${idRol}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(rolActualizado)
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                cerrarModalRol();
                mostrarAlerta("exito", "Actualizado", data.mensaje);
                F_GetRolesList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el rol.");
        });
}

// P_DeleteRol: confirma y ejecuta la baja logica de un rol.
function P_DeleteRol(idRol) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar rol?",
        "Esta accion marcara el rol como inactivo.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/RolesApi/P_DeleteRol/${idRol}`, {
                method: "DELETE"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Rol eliminado", data.mensaje);
                        F_GetRolesList();
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
