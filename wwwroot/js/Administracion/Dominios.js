// Obtiene el token antifalsificacion generado en VwDominios.cshtml.
// Lo consumen las peticiones POST, PUT y DELETE del CRUD.
let tablaDominios;

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

// Inicializacion de la vista: prepara controles, eventos y grilla principal.
document.addEventListener("DOMContentLoaded", () => {
    inicializarToggleDominio(true);
    inicializarToggleDominioPadre(false);
    configurarBotonDominio(false);

    document.getElementById("ddlDominio")?.addEventListener("change", () => {
        document.getElementById("txtIdDominio").value = "";
        configurarBotonDominio(false);
        F_GetDominiosList();
    });

    document.getElementById("BtnGuardarDominio")?.addEventListener("click", P_SaveDominio);
    document.getElementById("BtnNuevoDominio")?.addEventListener("click", () => limpiarFormularioDominio());

    F_GetDominiosList();
});

// Re-crea el toggle para reflejar el estado Vigente usando la misma libreria del proyecto.
function inicializarToggleDominio(activo) {
    setTimeout(() => {
        $('#chkDominioVigente').bootstrapToggle('destroy');
        $('#chkDominioVigente').bootstrapToggle({
            on: 'Activo',
            off: 'Inactivo',
            onstyle: 'primary',
            offstyle: 'danger',
            size: 'small',
            width: 92,
            height: 30
        });
        $('#chkDominioVigente').bootstrapToggle(activo ? 'on' : 'off');
    }, 50);
}

// Re-crea el toggle que indica si el dominio puede tener hijos.
function inicializarToggleDominioPadre(esPadre) {
    setTimeout(() => {
        $('#chkDominioPadre').bootstrapToggle('destroy');
        $('#chkDominioPadre').bootstrapToggle({
            on: 'Si',
            off: 'No',
            onstyle: 'primary',
            offstyle: 'danger',
            size: 'small',
            width: 92,
            height: 30
        });
        $('#chkDominioPadre').bootstrapToggle(esPadre ? 'on' : 'off');
    }, 50);
}

// Lee el dominio seleccionado en el dropdown.
function getDominioSeleccionado() {
    const value = document.getElementById("ddlDominio")?.value;
    return value ? Number(value) : 0;
}

// Cambia el texto/icono del boton principal segun el modo actual del formulario.
function configurarBotonDominio(esActualizacion) {
    const boton = document.getElementById("BtnGuardarDominio");
    if (!boton) return;

    boton.innerHTML = esActualizacion
        ? '<i class="fa-solid fa-pen-to-square me-2"></i> Actualizar'
        : '<i class="fa-solid fa-save me-2"></i> Guardar';
}

// F_GetDominiosList: arma la tabla de dominios y consulta los hijos del dominio seleccionado.
// Si no hay dominio seleccionado, deja la grilla vacia.
function F_GetDominiosList() {
    const idDominio = getDominioSeleccionado();

    if (!tablaDominios) {
        tablaDominios = Grilla({
            tableSelector: "#tablaDominios",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "dominio_Hijo",
            columns: [
                { title: "Dominio", data: "dominio", width: 30 },
                { title: "Subdominio", data: "dominio_Hijo", width: 28 },
                {
                    title: "DominioPadre?",
                    data: "dominioPadre",
                    width: 15,
                    className: "text-center",
                    searchable: false,
                    render: renderDominioPadre
                },
                {
                    title: "Activo",
                    data: "vigente",
                    width: 15,
                    className: "text-center",
                    searchable: false,
                    render: renderEstadoDominio
                }
            ],
            actions: {
                title: "Opciones",
                width: 10,
                items: [
                    {
                        action: "actualizar",
                        label: "Actualizar",
                        icon: "fa-solid fa-pen-to-square",
                        onClick: row => F_GetDominio(row.id_Dominio)
                    },
                    {
                        action: "eliminar",
                        label: "Eliminar",
                        icon: "fa-solid fa-trash",
                        onClick: row => P_DeleteDominio(row.id_Dominio)
                    }
                ]
            }
        });
    }

    if (!idDominio) {
        tablaDominios.setData([]);
        return;
    }

    secureFetch(`/api/DominiosApi/F_GetDominiosList/${idDominio}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            tablaDominios.setData(data.data);
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de dominios.");
        });
}

// Actualiza las opciones del dropdown usando el flujo general reutilizable.
function cargarDropdownDominios(idSeleccionado) {
    return secureFetch("/api/DominiosApi/F_GetDominiosDropdown")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const ddl = document.getElementById("ddlDominio");
            const seleccionado = idSeleccionado || "";

            ddl.textContent = "";

            const opcionInicial = document.createElement("option");
            opcionInicial.value = "";
            opcionInicial.textContent = "Seleccione una opcion";
            ddl.appendChild(opcionInicial);

            data.data.forEach(dominio => {
                const option = document.createElement("option");
                option.value = dominio.id_Dominio;
                option.textContent = dominio.descripcion;
                ddl.appendChild(option);
            });

            ddl.value = seleccionado;
        });
}

// Crea la etiqueta visual del estado del dominio.
function renderEstadoDominio(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

// Crea la etiqueta visual para indicar si el registro puede tener subdominios.
function renderDominioPadre(value) {
    const badge = document.createElement("span");
    const esPadre = value === "Si";
    badge.className = esPadre ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = esPadre ? "Si" : "No";
    return badge;
}

// P_SaveDominio: decide si el formulario debe insertar o actualizar segun txtIdDominio.
function P_SaveDominio() {
    limpiarErrores();

    const idDominio = document.getElementById("txtIdDominio").value;
    const idPadre = getDominioSeleccionado();
    const descripcion = document.getElementById("txtDescripcion").value.trim();

    if (!idPadre) {
        mostrarAlerta("advertencia", "Datos incompletos", "Selecciona un dominio.");
        return;
    }

    if (!descripcion) {
        mostrarError("txtDescripcion", "La descripcion es obligatoria");
        return;
    }

    if (descripcion.length < 2) {
        mostrarError("txtDescripcion", "La descripcion debe tener minimo 2 caracteres");
        return;
    }

    const payload = {
        id_Padre: idPadre,
        descripcion: descripcion,
        dominioPadre: document.getElementById("chkDominioPadre").checked ? "Si" : "No",
        vigente: document.getElementById("chkDominioVigente").checked ? 1 : 0
    };

    if (idDominio) {
        P_UdpDominio(idDominio, payload);
        return;
    }

    P_InsDominio(payload);
}

// P_InsDominio: registra un nuevo dominio hijo del dominio seleccionado.
function P_InsDominio(payload) {
    mostrarConfirmacion(
        "Registrar dominio?",
        "Verifica que los datos sean correctos.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch("/api/DominiosApi/P_InsDominio", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Registrado", data.mensaje);
                        cargarDropdownDominios().then(() => limpiarFormularioDominio());
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo procesar la solicitud.");
                });
        }
    );
}

// F_GetDominio: consulta un dominio por id y carga sus datos en modo actualizacion.
function F_GetDominio(idDominio) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/DominiosApi/F_GetDominio/${idDominio}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const dominio = data.data;

            document.getElementById("txtIdDominio").value = dominio.id_Dominio;
            document.getElementById("ddlDominio").value = dominio.id_Padre;
            document.getElementById("txtDescripcion").value = dominio.descripcion;
            inicializarToggleDominioPadre(dominio.dominioPadre === "Si");
            inicializarToggleDominio(dominio.vigente === 1);
            configurarBotonDominio(true);
            F_GetDominiosList();
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el dominio.");
        });
}

// P_UdpDominio: actualiza el dominio cargado en el formulario.
function P_UdpDominio(idDominio, payload) {
    mostrarConfirmacion(
        "Actualizar dominio?",
        "Verifica que los datos sean correctos.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/DominiosApi/P_UdpDominio/${idDominio}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Actualizado", data.mensaje);
                        cargarDropdownDominios().then(() => limpiarFormularioDominio());
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => {
                    mostrarAlerta("advertencia", "Error inesperado", "No se pudo procesar la actualizacion.");
                });
        }
    );
}

// Limpia el formulario y devuelve el dropdown a su opcion por defecto.
function limpiarFormularioDominio() {
    limpiarErrores();

    document.getElementById("txtIdDominio").value = "";
    document.getElementById("txtDescripcion").value = "";
    inicializarToggleDominioPadre(false);
    inicializarToggleDominio(true);
    configurarBotonDominio(false);

    const ddlDominio = document.getElementById("ddlDominio");
    ddlDominio.value = "";
    ddlDominio.dispatchEvent(new Event("change"));
}

// P_DeleteDominio: confirma y ejecuta la baja logica de un dominio.
function P_DeleteDominio(idDominio) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar dominio?",
        "Esta accion marcara el dominio como inactivo.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/DominiosApi/P_DeleteDominio/${idDominio}`, {
                method: "DELETE"
            })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Dominio eliminado", data.mensaje);
                        F_GetDominiosList();
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
