// Obtiene el token antifalsificacion generado en VwRegistrarPublicaciones.cshtml.
let tablaInicioContenidos;

function getCsrfToken() {
    return document.getElementById("csrfToken")?.value || "";
}

// Wrapper de fetch usado por el CRUD de inicio.
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

// Normaliza respuestas del API y maneja autenticacion/autorizacion.
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

document.addEventListener("DOMContentLoaded", () => {
    inicializarToggle("#chkMostrarInicio", true, "Si", "No");
    F_GetInicioContenidosList();

    document.getElementById("BtnRegistrarInicioContenido")?.addEventListener("click", P_InsInicioContenido);
    document.getElementById("BtnNuevoInicioContenido")?.addEventListener("click", limpiarFormularioInicioContenido);
    document.getElementById("ddlTipoContenido")?.addEventListener("change", () => configurarCamposPorTipoContenido("ddlTipoContenido", "txtContenidoInicio"));
    document.getElementById("ddlTipoContenidoModal")?.addEventListener("change", () => configurarCamposPorTipoContenido("ddlTipoContenidoModal", "ContenidoInicioModal"));
    document.querySelector("#modalEditarInicioContenido .btn-close-custom")?.addEventListener("click", cerrarModalInicioContenido);
    document.getElementById("btnCancelarInicioContenidoModal")?.addEventListener("click", cerrarModalInicioContenido);
    document.getElementById("btnGuardarInicioContenidoCambios")?.addEventListener("click", P_UdpInicioContenido);
    document.getElementById("btnSubirImagenInicio")?.addEventListener("click", () => P_UploadImagenInicio("fileImagenInicio", "txtImagenInicio", "previewImagenInicio", "ddlTipoContenido"));
    document.getElementById("btnSubirImagenInicioModal")?.addEventListener("click", () => P_UploadImagenInicio("fileImagenInicioModal", "ImagenInicioModal", "previewImagenInicioModal", "ddlTipoContenidoModal"));
    document.getElementById("txtImagenInicio")?.addEventListener("input", () => mostrarPreviewImagen("txtImagenInicio", "previewImagenInicio"));
    document.getElementById("ImagenInicioModal")?.addEventListener("input", () => mostrarPreviewImagen("ImagenInicioModal", "previewImagenInicioModal"));
    configurarCamposPorTipoContenido("ddlTipoContenido", "txtContenidoInicio");
});

// P_InsInicioContenido: valida el formulario y registra un contenido publico.
function P_InsInicioContenido() {
    limpiarErrores();
    const payload = obtenerPayloadFormulario();
    if (!payload) return;

    mostrarConfirmacion(
        "Registrar contenido?",
        "El contenido podra mostrarse en la pagina inicio si así lo configuro.",
        (confirmado) => {
            if (!confirmado) return;

            subirImagenSiPendiente("fileImagenInicio", "txtImagenInicio", "previewImagenInicio", "ddlTipoContenido")
                .then(imagenLista => {
                    if (!imagenLista) return;

                    payload.imagenUrl = valorCampo("txtImagenInicio");
                    return secureFetch("/api/RegistrarPublicacionesApi/P_InsInicioContenido", {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(payload)
                    });
                })
                .then(response => response ? parseJsonResponse(response) : null)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Registrado", data.mensaje);
                        limpiarFormularioInicioContenido();
                        F_GetInicioContenidosList();
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo registrar el contenido."));
        }
    );
}

// F_GetInicioContenidosList: arma la grilla administrativa y consulta los datos.
function F_GetInicioContenidosList() {
    if (!tablaInicioContenidos) {
        tablaInicioContenidos = Grilla({
            tableSelector: "#tablaInicioContenidos",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "tipoContenido",
            columns: [
                { title: "Tipo", data: "tipoContenido", width: 14 },
                { title: "Titulo", data: "titulo", width: 24 },
                { title: "Resumen", data: "resumen", width: 24 },
                { title: "Orden", data: "orden", width: 8, className: "text-center", searchable: false },
                { title: "Inicio", data: "mostrarEnInicio", width: 8, className: "text-center", searchable: false, render: renderSiNo },
                { title: "Estado", data: "vigente", width: 8, className: "text-center", searchable: false, render: renderEstado },
                { title: "Fecha creacion", data: "fecha_Creacion", width: 10, searchable: false, render: formatearFecha }
            ],
            actions: {
                title: "Opciones",
                width: 10,
                items: [
                    { action: "consultar", label: "Consultar", icon: "fa-solid fa-eye", onClick: consultarInicioContenido },
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetInicioContenido(row.id_InicioContenido) },
                    { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteInicioContenido(row.id_InicioContenido) }
                ]
            }
        });
    }

    secureFetch("/api/RegistrarPublicacionesApi/F_GetInicioContenidosList")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;
            tablaInicioContenidos.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de contenidos."));
}

// F_GetInicioContenido: consulta un registro y abre el modal de actualizacion.
function F_GetInicioContenido(idInicioContenido) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/RegistrarPublicacionesApi/F_GetInicioContenido/${idInicioContenido}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;

            const item = data.data;
            document.getElementById("Id_InicioContenido").value = item.id_InicioContenido;
            seleccionarDropdownPorTexto("ddlTipoContenidoModal", item.tipoContenido || "");
            document.getElementById("TituloInicioModal").value = item.titulo || "";
            document.getElementById("ResumenInicioModal").value = item.resumen || "";
            document.getElementById("ContenidoInicioModal").value = item.contenido || "";
            document.getElementById("ImagenInicioModal").value = item.imagenUrl || "";
            document.getElementById("EnlaceInicioModal").value = item.enlaceUrl || "";
            document.getElementById("TextoBotonInicioModal").value = item.textoBoton || "";
            document.getElementById("OrdenInicioModal").value = item.orden || 0;

            document.getElementById("ddlTipoContenidoModal").dispatchEvent(new Event("change"));
            configurarCamposPorTipoContenido("ddlTipoContenidoModal", "ContenidoInicioModal");
            mostrarPreviewImagen("ImagenInicioModal", "previewImagenInicioModal");
            abrirModalInicioContenido();
            inicializarToggle("#chkMostrarInicioModal", item.mostrarEnInicio == 1, "Si", "No");
            inicializarToggle("#chkInicioContenidoVigente", item.vigente == 1, "Activo", "Inactivo");
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el contenido."));
}

// P_UdpInicioContenido: actualiza el contenido seleccionado en el modal.
function P_UdpInicioContenido() {
    const idInicioContenido = document.getElementById("Id_InicioContenido").value;
    const payload = obtenerPayloadModal();
    if (!payload) return;

    subirImagenSiPendiente("fileImagenInicioModal", "ImagenInicioModal", "previewImagenInicioModal", "ddlTipoContenidoModal")
        .then(imagenLista => {
            if (!imagenLista) return null;

            payload.imagenUrl = valorCampo("ImagenInicioModal");
            return secureFetch(`/api/RegistrarPublicacionesApi/P_UdpInicioContenido/${idInicioContenido}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
        })
        .then(response => response ? parseJsonResponse(response) : null)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                cerrarModalInicioContenido();
                mostrarAlerta("exito", "Actualizado", data.mensaje);
                F_GetInicioContenidosList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo actualizar el contenido."));
}

// P_DeleteInicioContenido: confirma y ejecuta baja logica.
function P_DeleteInicioContenido(idInicioContenido) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion(
        "Eliminar contenido?",
        "Esta accion marcara el contenido como inactivo.",
        (confirmado) => {
            if (!confirmado) return;

            secureFetch(`/api/RegistrarPublicacionesApi/P_DeleteInicioContenido/${idInicioContenido}`, { method: "DELETE" })
                .then(parseJsonResponse)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Contenido eliminado", data.mensaje);
                        F_GetInicioContenidosList();
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el contenido."));
        }
    );
}

// P_UploadImagenInicio: sube una imagen al servidor y coloca su ruta publica en el formulario.
function P_UploadImagenInicio(inputArchivoId, inputRutaId, previewId, inputTipoContenidoId) {
    subirImagenSiPendiente(inputArchivoId, inputRutaId, previewId, inputTipoContenidoId, true);
}

// subirImagenSiPendiente: si hay archivo seleccionado lo sube antes de guardar.
// Si no hay archivo, permite continuar usando la ruta que ya este escrita.
function subirImagenSiPendiente(inputArchivoId, inputRutaId, previewId, inputTipoContenidoId, mostrarExito = false) {
    const inputArchivo = document.getElementById(inputArchivoId);
    const archivo = inputArchivo?.files?.[0];
    const idTipoContenido = Number(document.getElementById(inputTipoContenidoId)?.value || 0);

    if (!archivo) {
        return Promise.resolve(true);
    }

    if (!idTipoContenido) {
        mostrarAlerta("advertencia", "Tipo requerido", "Selecciona el tipo de contenido antes de subir la imagen.");
        return Promise.resolve(false);
    }

    const formData = new FormData();
    formData.append("imagen", archivo);
    formData.append("idTipoContenido", idTipoContenido);

    return secureFetch("/api/RegistrarPublicacionesApi/P_UploadImagenInicio", {
        method: "POST",
        body: formData
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                document.getElementById(inputRutaId).value = data.data;
                mostrarPreviewImagen(inputRutaId, previewId);
                inputArchivo.value = "";
                if (mostrarExito) {
                    mostrarAlerta("exito", "Imagen cargada", data.mensaje);
                }
                return true;
            }

            mostrarAlerta("error", "No fue posible subir", data.mensaje);
            return false;
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo subir la imagen.");
            return false;
        });
}

function obtenerPayloadFormulario() {
    const selectTipoContenido = document.getElementById("ddlTipoContenido");
    const idTipoContenido = Number(selectTipoContenido.value || 0);
    const tipoContenido = obtenerTextoSeleccionado(selectTipoContenido);
    const titulo = document.getElementById("txtTituloInicio").value.trim();

    if (!idTipoContenido) { mostrarError("ddlTipoContenido", "El tipo es obligatorio"); return null; }
    if (!titulo) { mostrarError("txtTituloInicio", "El titulo es obligatorio"); return null; }

    return {
        idTipoContenido,
        tipoContenido,
        titulo,
        resumen: valorCampo("txtResumenInicio"),
        contenido: valorCampo("txtContenidoInicio"),
        imagenUrl: valorCampo("txtImagenInicio"),
        enlaceUrl: valorCampo("txtEnlaceInicio"),
        textoBoton: valorCampo("txtTextoBotonInicio"),
        mostrarEnInicio: document.getElementById("chkMostrarInicio").checked ? 1 : 0,
        orden: Number(document.getElementById("txtOrdenInicio").value || 0)
    };
}

function obtenerPayloadModal() {
    const selectTipoContenido = document.getElementById("ddlTipoContenidoModal");
    const idTipoContenido = Number(selectTipoContenido.value || 0);
    const tipoContenido = obtenerTextoSeleccionado(selectTipoContenido);
    const titulo = document.getElementById("TituloInicioModal").value.trim();

    if (!idTipoContenido || !titulo) {
        mostrarAlerta("advertencia", "Datos incompletos", "Tipo de contenido y titulo son obligatorios.");
        return null;
    }

    return {
        idTipoContenido,
        tipoContenido,
        titulo,
        resumen: valorCampo("ResumenInicioModal"),
        contenido: valorCampo("ContenidoInicioModal"),
        imagenUrl: valorCampo("ImagenInicioModal"),
        enlaceUrl: valorCampo("EnlaceInicioModal"),
        textoBoton: valorCampo("TextoBotonInicioModal"),
        mostrarEnInicio: document.getElementById("chkMostrarInicioModal").checked ? 1 : 0,
        orden: Number(document.getElementById("OrdenInicioModal").value || 0),
        vigente: document.getElementById("chkInicioContenidoVigente").checked ? 1 : 0
    };
}

function valorCampo(id) {
    return document.getElementById(id).value.trim() || null;
}

// configurarCamposPorTipoContenido: ahora SIEMPRE permite escribir contenido.
function configurarCamposPorTipoContenido(idSelect, idContenido) {
    const contenido = document.getElementById(idContenido);
    if (!contenido) return;

    contenido.disabled = false;
    contenido.classList.remove("field-disabled-by-rule");
    contenido.placeholder = " ";
}

function obtenerTextoSeleccionado(select) {
    return select.options[select.selectedIndex]?.text?.trim() || "";
}

function seleccionarDropdownPorTexto(idSelect, texto) {
    const select = document.getElementById(idSelect);
    const textoNormalizado = texto.trim().toLowerCase();
    const option = Array.from(select.options).find(item => item.text.trim().toLowerCase() === textoNormalizado);
    select.value = option?.value || "";
}

function consultarInicioContenido(item) {
    mostrarAlerta(
        "info",
        "Detalle del contenido",
        `Tipo: ${item.tipoContenido || ""}\nTitulo: ${item.titulo || ""}\nResumen: ${item.resumen || ""}\nMostrar en inicio: ${item.mostrarEnInicio == 1 ? "Si" : "No"}\nEstado: ${item.vigente == 1 ? "Activo" : "Inactivo"}`
    );
}

function limpiarFormularioInicioContenido() {
    document.getElementById("ddlTipoContenido").value = "";
    document.getElementById("txtTituloInicio").value = "";
    document.getElementById("txtResumenInicio").value = "";
    document.getElementById("txtContenidoInicio").value = "";
    document.getElementById("txtImagenInicio").value = "";
    document.getElementById("fileImagenInicio").value = "";
    document.getElementById("txtEnlaceInicio").value = "";
    document.getElementById("txtTextoBotonInicio").value = "";
    document.getElementById("txtOrdenInicio").value = "0";
    document.getElementById("ddlTipoContenido").dispatchEvent(new Event("change"));
    configurarCamposPorTipoContenido("ddlTipoContenido", "txtContenidoInicio");
    ocultarPreviewImagen("previewImagenInicio");
    inicializarToggle("#chkMostrarInicio", true, "Si", "No");
}

function abrirModalInicioContenido() {
    document.getElementById("modalEditarInicioContenido").style.display = "flex";
}

function cerrarModalInicioContenido() {
    document.getElementById("modalEditarInicioContenido").style.display = "none";
    document.getElementById("fileImagenInicioModal").value = "";
}

function mostrarPreviewImagen(inputRutaId, previewId) {
    const ruta = document.getElementById(inputRutaId).value.trim();
    const preview = document.getElementById(previewId);

    if (!ruta) {
        ocultarPreviewImagen(previewId);
        return;
    }

    preview.src = ruta;
    preview.classList.remove("d-none");
}

function ocultarPreviewImagen(previewId) {
    const preview = document.getElementById(previewId);
    preview.removeAttribute("src");
    preview.classList.add("d-none");
}

function formatearFecha(fecha) {
    if (!fecha) return "";
    return new Date(fecha).toLocaleString();
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

function renderSiNo(valor) {
    const badge = document.createElement("span");
    badge.className = valor == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = valor == 1 ? "Si" : "No";
    return badge;
}

function inicializarToggle(selector, activo, textoActivo, textoInactivo) {
    setTimeout(() => {
        $(selector).bootstrapToggle('destroy');
        $(selector).bootstrapToggle({
            on: textoActivo,
            off: textoInactivo,
            onstyle: 'primary',
            offstyle: 'danger',
            size: 'small',
            width: 92,
            height: 30
        });
        $(selector).bootstrapToggle(activo ? 'on' : 'off');
    }, 50);
}
