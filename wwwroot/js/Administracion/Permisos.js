// Obtiene el token antifalsificacion generado en Permisos.cshtml.
// Lo consumen las peticiones POST, PUT y DELETE del CRUD.
let tablaPermisos;
let tablaRolesPermiso;
let rolesPermiso = [];

function getCsrfToken() {
    return document.getElementById("csrfToken")?.value || "";
}

// Wrapper de fetch usado por todo este archivo.
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

// Normaliza respuestas del API y maneja errores de autenticacion/autorizacion.
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

// Inicializacion de la vista: carga permisos y conecta botones del formulario/modal.
document.addEventListener("DOMContentLoaded", () => {
    inicializarTablaRolesPermiso();
    F_GetPermisosList();

    document.getElementById("BtnRegistrarPermiso")?.addEventListener("click", P_InsPermiso);
    document.querySelector("#modalEditarPermiso .btn-close-custom")?.addEventListener("click", cerrarModalPermiso);
    document.getElementById("btnCancelarPermisoModal")?.addEventListener("click", cerrarModalPermiso);
    document.getElementById("btnGuardarPermisoCambios")?.addEventListener("click", P_UdpPermiso);

    document.querySelector("#modalAsignarPermisoRol .btn-close-custom")?.addEventListener("click", cerrarModalAsignarPermisoRol);
    document.getElementById("btnCancelarAsignarPermisoRol")?.addEventListener("click", cerrarModalAsignarPermisoRol);
    document.getElementById("btnGuardarAsignarPermisoRol")?.addEventListener("click", guardarPermisoRol);
    document.getElementById("btnActivarTodosRolesPermiso")?.addEventListener("click", () => cambiarTodosRolesPermiso(true));
    document.getElementById("btnInactivarTodosRolesPermiso")?.addEventListener("click", () => cambiarTodosRolesPermiso(false));
});

// Inicializa la grilla reutilizable del modal de roles por permiso.
function inicializarTablaRolesPermiso() {
    if (tablaRolesPermiso) return;

    tablaRolesPermiso = Grilla({
        tableSelector: "#tablaRolesPermiso",
        search: false,
        sorting: true,
        pagination: false,
        pageSize: false,
        info: false,
        defaultSortKey: "rol",
        emptyText: "Seleccione un permiso para consultar sus roles.",
        columns: [
            { title: "Rol", data: "rol", width: 60 },
            {
                title: "Permiso activo",
                data: "vigente",
                width: 30,
                className: "text-center",
                headerClassName: "text-center",
                searchable: false,
                sortable: false,
                render: renderToggleRolPermiso
            }
        ]
    });

    tablaRolesPermiso.setData([]);
}

// Renderiza el switch de asignacion de rol dentro de la grilla estandar.
function renderToggleRolPermiso(_vigente, rol) {
    const container = document.createElement("div");
    container.className = "toggle-container roles-permiso-toggle";

    const input = document.createElement("input");
    input.type = "checkbox";
    input.className = "chk-rol-permiso";
    input.id = `chkRolPermiso_${rol.id_Rol}`;
    input.dataset.idRol = rol.id_Rol;

    container.appendChild(input);
    return container;
}

// P_InsPermiso: valida el formulario superior y registra un permiso de menu.
function P_InsPermiso() {
    limpiarErrores();

    const id_Menu = Number(obtenerValorSelect("ddlMenuPermiso"));
    const modulo = obtenerTextoSelect("ddlMenuPermiso");
    const accion = obtenerValorSelect("ddlAccionPermiso");
    const descripcion = document.getElementById("txtDescripcionPermiso").value.trim();

    let valido = true;
    if (!id_Menu) { mostrarError("ddlMenuPermiso", "El menu es obligatorio"); valido = false; }
    if (!accion) { mostrarError("ddlAccionPermiso", "La accion es obligatoria"); valido = false; }
    if (!valido) return;

    mostrarConfirmacion(
        "Registrar permiso?",
        "Verifica que el menu y la accion sean correctos.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch("/api/PermisosApi/P_InsPermiso", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ id_Menu, modulo, accion, descripcion })
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Permiso registrado", data.mensaje);
                        limpiarFormularioPermiso();
                        F_GetPermisosList();
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

// Obtiene el valor seleccionado en un dropdown.
function obtenerValorSelect(idSelect) {
    return document.getElementById(idSelect)?.value?.trim() || "";
}

// Obtiene el texto visible de un dropdown.
function obtenerTextoSelect(idSelect) {
    const select = document.getElementById(idSelect);
    return select?.options[select.selectedIndex]?.text?.trim() || "";
}

// Escapa texto antes de insertarlo como HTML en elementos creados dinamicamente.
function escapeHtml(valor) {
    return String(valor || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Limpia el formulario superior despues de una creacion exitosa o desde Nuevo.
function limpiarFormularioPermiso() {
    document.getElementById("ddlMenuPermiso").value = "";
    document.getElementById("ddlAccionPermiso").value = "";
    document.getElementById("txtDescripcionPermiso").value = "";

    document.getElementById("ddlMenuPermiso").dispatchEvent(new Event("change"));
    document.getElementById("ddlAccionPermiso").dispatchEvent(new Event("change"));
}

// F_GetPermisosList: arma la grilla de permisos y consulta los datos que la alimentan.
function F_GetPermisosList() {
    if (!tablaPermisos) {
        tablaPermisos = Grilla({
            tableSelector: "#tablaPermisos",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "modulo",
            columns: [
                { title: "Menu", data: "menu", width: 22 },
                { title: "Accion", data: "accion", width: 16 },
                { title: "Descripcion", data: "descripcion", width: 30 },
                {
                    title: "Estado",
                    data: "vigente",
                    width: 10,
                    className: "text-center",
                    searchable: false,
                    render: renderEstadoPermiso
                },
                {
                    title: "Fecha creacion",
                    data: "fecha_Creacion",
                    width: 12,
                    searchable: false,
                    render: formatearFecha
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
                        onClick: consultarPermiso
                    },
                    {
                        action: "asignarPermisoRol",
                        label: "Asignar Permiso a Rol",
                        icon: "fa-solid fa-user-shield",
                        onClick: abrirModalAsignarPermisoRol
                    },
                    {
                        action: "actualizarPermiso",
                        label: "Actualizar Permiso",
                        icon: "fa-solid fa-pen-to-square",
                        onClick: row => F_GetPermiso(row.id_Permiso)
                    },
                    {
                        action: "eliminarPermiso",
                        label: "Eliminar Permiso",
                        icon: "fa-solid fa-trash",
                        onClick: row => P_DeletePermiso(row.id_Permiso)
                    }
                ]
            }
        });
    }

    secureFetch("/api/PermisosApi/F_GetPermisosList")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            tablaPermisos.setData(data.data);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de permisos.");
        });
}

function formatearFecha(fecha) {
    if (!fecha) return "";
    return new Date(fecha).toLocaleString();
}

// Crea la etiqueta visual del estado del permiso.
function renderEstadoPermiso(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

// Muestra los datos principales del permiso sin abrir modal.
function consultarPermiso(permiso) {
    mostrarAlerta(
        "info",
        "Detalle del permiso",
        `Menu: ${permiso.menu || permiso.modulo || ""}, Accion: ${permiso.accion || ""}, Descripcion: ${permiso.descripcion || ""}, Estado: ${permiso.vigente == 1 ? "Activo" : "Inactivo"}`
    );
}

// F_GetPermiso: consulta un permiso por id y carga el modal de actualizacion.
function F_GetPermiso(idPermiso) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/PermisosApi/F_GetPermiso/${idPermiso}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const permiso = data.data;
            document.getElementById("Id_Permiso").value = permiso.id_Permiso;
            seleccionarValorDropdown("Modulo", permiso.id_Menu, permiso.modulo);
            seleccionarValorDropdown("Accion", permiso.accion);
            document.getElementById("DescripcionPermiso").value = permiso.descripcion || "";

            abrirModalPermiso();
            inicializarToggle("#chkPermisoVigente", permiso.vigente === 1);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el permiso.");
        });
}

// Selecciona un valor existente. Si el valor viene de datos antiguos y no esta
// en el dropdown actual, lo agrega temporalmente para no ocultarlo en edicion.
function seleccionarValorDropdown(idSelect, valor, textoAlterno) {
    const select = document.getElementById(idSelect);
    const valorNormalizado = valor ? String(valor) : "";
    const existe = Array.from(select.options).some(option => option.value === valorNormalizado);

    if (valorNormalizado && !existe) {
        const option = document.createElement("option");
        option.value = valorNormalizado;
        option.textContent = textoAlterno || valorNormalizado;
        select.appendChild(option);
    }

    select.value = valorNormalizado;
    select.dispatchEvent(new Event("change"));
}

function abrirModalPermiso() {
    document.getElementById("modalEditarPermiso").style.display = "flex";
}

function cerrarModalPermiso() {
    document.getElementById("modalEditarPermiso").style.display = "none";
}

// Abre el modal de asignacion usando el permiso seleccionado en la tabla.
function abrirModalAsignarPermisoRol(permiso) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    document.getElementById("Id_Permiso_Asignar").value = permiso.id_Permiso;
    document.getElementById("NombrePermisoAsignar").value = `${permiso.menu || permiso.modulo || ""} - ${permiso.accion || ""}`;
    rolesPermiso = [];
    pintarRolesPermisoCargando();

    document.getElementById("modalAsignarPermisoRol").style.display = "flex";
    F_GetRolesPorPermiso(permiso.id_Permiso);
}

function cerrarModalAsignarPermisoRol() {
    document.getElementById("modalAsignarPermisoRol").style.display = "none";
    document.getElementById("Id_Permiso_Asignar").value = "";
    document.getElementById("NombrePermisoAsignar").value = "";
    rolesPermiso = [];
    pintarRolesPermisoVacio("Seleccione un permiso para consultar sus roles.");
}

// F_GetRolesPorPermiso: consulta todos los roles asignables y el estado actual del permiso en cada uno.
function F_GetRolesPorPermiso(idPermiso) {
    secureFetch(`/api/PermisosApi/F_GetRolesPorPermiso/${idPermiso}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) {
                rolesPermiso = [];
                pintarRolesPermisoVacio(data?.mensaje || "No fue posible consultar los roles.");
                return;
            }

            rolesPermiso = data.data || [];
            pintarRolesPermiso(rolesPermiso);
        })
        .catch(() => {
            rolesPermiso = [];
            pintarRolesPermisoVacio("No se pudo consultar la lista de roles.");
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar la lista de roles.");
        });
}

// Muestra una fila de carga mientras llega la informacion del API.
function pintarRolesPermisoCargando() {
    pintarRolesPermisoVacio("Consultando roles asignables...");
}

// Muestra un mensaje dentro de la tabla cuando no hay datos para presentar.
function pintarRolesPermisoVacio(mensaje) {
    inicializarTablaRolesPermiso();
    tablaRolesPermiso.setEmptyText(mensaje);
    tablaRolesPermiso.setData([]);
}

// Construye la tabla de roles y genera un switch por cada rol asignable.
function pintarRolesPermiso(roles) {
    inicializarTablaRolesPermiso();

    if (!roles.length) {
        pintarRolesPermisoVacio("No hay roles asignables para este permiso.");
        return;
    }

    tablaRolesPermiso.setEmptyText("No hay roles asignables para este permiso.");
    tablaRolesPermiso.setData(roles);
    roles.forEach(rol => inicializarToggle(`#chkRolPermiso_${rol.id_Rol}`, rol.vigente == 1));
}

// Cambia todos los switches visibles del modal sin guardar todavia.
function cambiarTodosRolesPermiso(activo) {
    document.querySelectorAll("#tablaRolesPermiso .chk-rol-permiso").forEach(input => {
        $(input).bootstrapToggle(activo ? "on" : "off");
    });
}

// guardarPermisoRol: envia el estado completo de todos los roles para el permiso seleccionado.
function guardarPermisoRol() {
    const idPermiso = document.getElementById("Id_Permiso_Asignar").value;

    if (!idPermiso || !rolesPermiso.length) {
        mostrarAlerta("advertencia", "Datos incompletos", "No hay roles para guardar en este permiso.");
        return;
    }

    const roles = rolesPermiso.map(rol => ({
        id_Rol: rol.id_Rol,
        vigente: document.getElementById(`chkRolPermiso_${rol.id_Rol}`)?.checked ? 1 : 0
    }));

    mostrarConfirmacion(
        "Guardar permisos por rol?",
        "Se actualizaran las asignaciones activas e inactivas para este permiso.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/PermisosApi/P_UdpRolesPermiso/${idPermiso}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ roles })
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        cerrarModalAsignarPermisoRol();
                        mostrarAlerta("exito", "Permisos actualizados", data.mensaje);
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudieron guardar los permisos por rol.");
                });
        }
    );
}

// P_UdpPermiso: toma los datos del modal y actualiza el permiso seleccionado.
function P_UdpPermiso() {
    const idPermiso = document.getElementById("Id_Permiso").value;
    const id_Menu = Number(obtenerValorSelect("Modulo"));
    const modulo = obtenerTextoSelect("Modulo");
    const accion = obtenerValorSelect("Accion");
    const descripcion = document.getElementById("DescripcionPermiso").value.trim();

    if (!id_Menu || !accion) {
        mostrarAlerta("advertencia", "Datos incompletos", "Menu y accion son obligatorios.");
        return;
    }

    secureFetch(`/api/PermisosApi/P_UdpPermiso/${idPermiso}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            modulo,
            id_Menu,
            accion,
            descripcion,
            vigente: document.getElementById("chkPermisoVigente").checked ? 1 : 0
        })
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                cerrarModalPermiso();
                mostrarAlerta("exito", "Actualizado", data.mensaje);
                F_GetPermisosList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el permiso.");
        });
}

// P_DeletePermiso: confirma y ejecuta la baja logica del permiso.
function P_DeletePermiso(idPermiso) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar permiso?",
        "Esta accion inactivara el permiso y sus asignaciones activas.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/PermisosApi/P_DeletePermiso/${idPermiso}`, {
                method: "DELETE"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Permiso eliminado", data.mensaje);
                        F_GetPermisosList();
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

// Inicializa un toggle bootstrap con el estado indicado.
function inicializarToggle(selector, activo) {
    setTimeout(() => {
        $(selector).bootstrapToggle('destroy');
        $(selector).bootstrapToggle({
            on: 'Activo',
            off: 'Inactivo',
            onstyle: 'primary',
            offstyle: 'danger',
            size: 'small',
            width: 92,
            height: 30
        });
        $(selector).bootstrapToggle(activo ? 'on' : 'off');
    }, 50);
}
