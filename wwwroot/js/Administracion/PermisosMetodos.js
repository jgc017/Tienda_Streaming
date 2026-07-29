// Maneja la seccion de permisos automaticos de metodos dentro de VwPermisos.
let tablaPermisosMetodos;

document.addEventListener("DOMContentLoaded", () => {
    F_GetPermisosMetodosList();

    document.getElementById("BtnSincronizarPermisosMetodos")?.addEventListener("click", P_SyncPermisosMetodos);
    document.querySelector("#modalEditarPermisoMetodo .btn-close-custom")?.addEventListener("click", cerrarModalPermisoMetodo);
    document.getElementById("btnCancelarPermisoMetodoModal")?.addEventListener("click", cerrarModalPermisoMetodo);
    document.getElementById("btnGuardarPermisoMetodoCambios")?.addEventListener("click", P_UdpPermisoMetodo);
});

// P_SyncPermisosMetodos: solicita al servidor escanear controladores API y
// crear/actualizar permisos de metodo automaticamente.
function P_SyncPermisosMetodos() {
    mostrarConfirmacion(
        "Sincronizar metodos?",
        "Se revisaran los controladores API y se actualizaran los permisos de metodos detectados.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch("/api/PermisosMetodosApi/P_SyncPermisosMetodos", {
                method: "POST"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Sincronizacion completada", data.mensaje);
                        F_GetPermisosMetodosList();
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo sincronizar la lista de metodos.");
                });
        }
    );
}

// F_GetPermisosMetodosList: arma la grilla y consulta permisos de metodos.
function F_GetPermisosMetodosList() {
    if (!tablaPermisosMetodos) {
        tablaPermisosMetodos = Grilla({
            tableSelector: "#tablaPermisosMetodos",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "vista",
            columns: [
                { title: "Formulario", data: "formulario", width: 14, render: valor => valor || "Sin asociar" },
                { title: "Vista", data: "vista", width: 14, render: valor => valor || "Sin asociar" },
                { title: "Accion", data: "accion", width: 9 },
                { title: "Controlador API", data: "controlador", width: 14 },
                { title: "Metodo", data: "metodo", width: 18 },
                { title: "HTTP", data: "httpMetodo", width: 6, className: "text-center" },
                { title: "Descripcion", data: "descripcion", width: 18 },
                {
                    title: "Estado",
                    data: "vigente",
                    width: 5,
                    className: "text-center",
                    searchable: false,
                    render: renderEstadoPermiso
                }
            ],
            actions: {
                title: "Opciones",
                width: 6,
                items: [
                    {
                        action: "consultarMetodo",
                        label: "Consultar",
                        icon: "fa-solid fa-eye",
                        onClick: consultarPermisoMetodo
                    },
                    {
                        action: "asignarMetodoRol",
                        label: "Asignar Permiso a Rol",
                        icon: "fa-solid fa-user-shield",
                        onClick: abrirModalAsignarPermisoRol
                    },
                    {
                        action: "actualizarMetodo",
                        label: "Actualizar Permiso",
                        icon: "fa-solid fa-pen-to-square",
                        onClick: row => F_GetPermisoMetodo(row.id_Permiso)
                    },
                    {
                        action: "eliminarMetodo",
                        label: "Eliminar Permiso",
                        icon: "fa-solid fa-trash",
                        onClick: row => P_DeletePermisoMetodo(row.id_Permiso)
                    }
                ]
            }
        });
    }

    secureFetch("/api/PermisosMetodosApi/F_GetPermisosMetodosList")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            tablaPermisosMetodos.setData(data.data);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de permisos de metodos.");
        });
}

// Muestra informacion tecnica del permiso de metodo seleccionado.
function consultarPermisoMetodo(permiso) {
    mostrarAlerta(
        "info",
        "Detalle del permiso de metodo",
        `Formulario: ${permiso.formulario || "Sin asociar"}, Vista: ${permiso.vista || "Sin asociar"}, Accion: ${permiso.accion || ""}, Controlador API: ${permiso.controlador || ""}, Metodo: ${permiso.metodo || ""}, HTTP: ${permiso.httpMetodo || ""}, Estado: ${permiso.vigente == 1 ? "Activo" : "Inactivo"}`
    );
}

// F_GetPermisoMetodo: consulta un permiso de metodo y carga el modal.
function F_GetPermisoMetodo(idPermiso) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/PermisosMetodosApi/F_GetPermisoMetodo/${idPermiso}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const permiso = data.data;
            document.getElementById("Id_Permiso_Metodo").value = permiso.id_Permiso;
            document.getElementById("VistaPermisoMetodo").value = permiso.vista || "Sin asociar";
            document.getElementById("ControladorMetodo").value = permiso.controlador || "";
            document.getElementById("MetodoPermiso").value = permiso.metodo || "";
            document.getElementById("HttpMetodoPermiso").value = permiso.httpMetodo || "";
            document.getElementById("DescripcionPermisoMetodo").value = permiso.descripcion || "";

            abrirModalPermisoMetodo();
            inicializarToggle("#chkPermisoMetodoVigente", permiso.vigente === 1);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el permiso de metodo.");
        });
}

function abrirModalPermisoMetodo() {
    document.getElementById("modalEditarPermisoMetodo").style.display = "flex";
}

function cerrarModalPermisoMetodo() {
    document.getElementById("modalEditarPermisoMetodo").style.display = "none";
}

// P_UdpPermisoMetodo: actualiza descripcion y estado del permiso de metodo.
function P_UdpPermisoMetodo() {
    const idPermiso = document.getElementById("Id_Permiso_Metodo").value;
    const descripcion = document.getElementById("DescripcionPermisoMetodo").value.trim();

    secureFetch(`/api/PermisosMetodosApi/P_UdpPermisoMetodo/${idPermiso}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            descripcion,
            vigente: document.getElementById("chkPermisoMetodoVigente").checked ? 1 : 0
        })
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                cerrarModalPermisoMetodo();
                mostrarAlerta("exito", "Actualizado", data.mensaje);
                F_GetPermisosMetodosList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el permiso de metodo.");
        });
}

// P_DeletePermisoMetodo: confirma e inactiva el permiso y sus asignaciones.
function P_DeletePermisoMetodo(idPermiso) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar permiso de metodo?",
        "Esta accion inactivara el permiso de metodo y sus asignaciones activas.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/PermisosMetodosApi/P_DeletePermisoMetodo/${idPermiso}`, {
                method: "DELETE"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Permiso eliminado", data.mensaje);
                        F_GetPermisosMetodosList();
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
