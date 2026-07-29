let tablaImagenesProducto;

function getCsrfToken() {
    return document.getElementById("csrfToken")?.value || "";
}

function secureFetch(url, options = {}) {
    const method = (options.method || "GET").toUpperCase();
    const headers = new Headers(options.headers || {});

    if (method !== "GET" && method !== "HEAD" && method !== "OPTIONS") {
        headers.set("X-CSRF-TOKEN", getCsrfToken());
    }

    return fetch(url, { ...options, headers, credentials: "same-origin" });
}

async function parseJsonResponse(response) {
    if (response.status === 401) {
        window.location.href = "/Account/Login";
        return null;
    }

    if (response.status === 403) {
        mostrarAlerta("advertencia", "Acceso denegado", "No tienes permisos para realizar esta accion.");
        return null;
    }

    return await response.json().catch(() => null);
}

document.addEventListener("DOMContentLoaded", () => {
    inicializarToggleImagenProducto(true);
    document.getElementById("BtnGuardarImagenProducto")?.addEventListener("click", P_SaveImagenProducto);
    document.getElementById("BtnNuevoImagenProducto")?.addEventListener("click", limpiarFormularioImagenProducto);
    document.getElementById("btnSubirImagenProducto")?.addEventListener("click", P_UploadImagenProducto);
    document.getElementById("txtImagenProducto")?.addEventListener("input", mostrarPreviewImagenProducto);
    document.getElementById("fileImagenProducto")?.addEventListener("change", mostrarPreviewImagenProductoDesdeArchivo);
    F_GetImagenesProductoList();
});

function F_GetImagenesProductoList() {
    if (!tablaImagenesProducto) {
        tablaImagenesProducto = Grilla({
            tableSelector: "#tablaImagenesProducto",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "orden",
            columns: [
                { title: "Orden", data: "orden", width: 7, className: "text-center", searchable: false },
                { title: "Tipo", data: "tipoImagen", width: 12 },
                { title: "Plataforma", data: "plataforma", width: 18 },
                { title: "Imagen", data: "imagenUrl", width: 24 },
                { title: "Descripcion", data: "descripcion", width: 20 },
                { title: "Estado", data: "vigente", width: 10, className: "text-center", searchable: false, render: renderEstado },
                { title: "Fecha creacion", data: "fecha_Creacion", width: 12, searchable: false, render: formatearFecha }
            ],
            actions: {
                title: "Opciones",
                width: 8,
                items: [
                    { action: "subir", label: "Subir", icon: "fa-solid fa-arrow-up", onClick: row => P_MoverImagenProducto(row.id_ImagenProducto, -1) },
                    { action: "bajar", label: "Bajar", icon: "fa-solid fa-arrow-down", onClick: row => P_MoverImagenProducto(row.id_ImagenProducto, 1) },
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetImagenProducto(row.id_ImagenProducto) },
                    { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteImagenProducto(row.id_ImagenProducto) }
                ]
            }
        });
    }

    secureFetch("/api/ImagenesProductoApi/F_GetImagenesProductoList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaImagenesProducto.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de imagenes."));
}

function P_SaveImagenProducto() {
    limpiarErrores();
    const idImagen = document.getElementById("txtIdImagenProducto").value;
    const payload = obtenerPayloadImagenProducto();
    if (!payload) return;

    subirImagenSiPendiente().then(imagenLista => {
        if (!imagenLista) return;

        payload.imagenUrl = document.getElementById("txtImagenProducto").value.trim();
        const esActualizacion = Boolean(idImagen);
        return secureFetch(esActualizacion ? `/api/ImagenesProductoApi/P_UdpImagenProducto/${idImagen}` : "/api/ImagenesProductoApi/P_InsImagenProducto", {
            method: esActualizacion ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });
    })
        .then(response => response ? parseJsonResponse(response) : null)
        .then(data => {
            if (!data) return;
            if (data.ok) {
                mostrarAlerta("exito", "Guardado", data.mensaje);
                limpiarFormularioImagenProducto();
                F_GetImagenesProductoList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar la imagen."));
}

function F_GetImagenProducto(idImagen) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/ImagenesProductoApi/F_GetImagenProducto/${idImagen}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data?.ok) return;
            const item = data.data;
            document.getElementById("txtIdImagenProducto").value = item.id_ImagenProducto;
            document.getElementById("ddlPlataformaImagen").value = item.id_Plataforma;
            document.getElementById("ddlTipoImagenProducto").value = item.id_Tipo_Imagen || "";
            document.getElementById("txtImagenProducto").value = item.imagenUrl || "";
            document.getElementById("txtDescripcionImagenProducto").value = item.descripcion || "";
            inicializarToggleImagenProducto(item.vigente == 1);
            mostrarPreviewImagenProducto();
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la imagen."));
}

function P_DeleteImagenProducto(idImagen) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion("Eliminar imagen?", "Esta accion marcara la imagen como inactiva.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/ImagenesProductoApi/P_DeleteImagenProducto/${idImagen}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Imagen eliminada", data.mensaje);
                    F_GetImagenesProductoList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar la imagen."));
    });
}

function P_MoverImagenProducto(idImagen, direccion) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch("/api/ImagenesProductoApi/P_MoverImagenProducto", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            id_ImagenProducto: idImagen,
            direccion
        })
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;
            if (data.ok) {
                F_GetImagenesProductoList();
            } else {
                mostrarAlerta("error", "Error", data.mensaje);
            }
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo actualizar el orden."));
}

function P_UploadImagenProducto() {
    subirImagenSiPendiente(true);
}

function subirImagenSiPendiente(mostrarExito = false) {
    const inputArchivo = document.getElementById("fileImagenProducto");
    const archivo = inputArchivo?.files?.[0];

    if (!archivo) {
        return Promise.resolve(true);
    }

    const formData = new FormData();
    formData.append("imagen", archivo);

    return secureFetch("/api/ImagenesProductoApi/P_UploadImagenProducto", {
        method: "POST",
        body: formData
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return false;
            if (data.ok) {
                document.getElementById("txtImagenProducto").value = data.data;
                inputArchivo.value = "";
                mostrarPreviewImagenProducto();
                if (mostrarExito) mostrarAlerta("exito", "Imagen cargada", data.mensaje);
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

function obtenerPayloadImagenProducto() {
    const idPlataforma = Number(document.getElementById("ddlPlataformaImagen").value || 0);
    const idTipoImagen = Number(document.getElementById("ddlTipoImagenProducto").value || 0);
    const imagenUrl = document.getElementById("txtImagenProducto").value.trim();

    if (!idPlataforma) { mostrarError("ddlPlataformaImagen", "La plataforma es obligatoria"); return null; }
    if (!idTipoImagen) { mostrarError("ddlTipoImagenProducto", "El tipo de imagen es obligatorio"); return null; }
    if (!imagenUrl && !document.getElementById("fileImagenProducto")?.files?.length) {
        mostrarError("txtImagenProducto", "La imagen es obligatoria");
        return null;
    }

    return {
        id_Plataforma: idPlataforma,
        id_Tipo_Imagen: idTipoImagen,
        imagenUrl,
        descripcion: document.getElementById("txtDescripcionImagenProducto").value.trim() || null,
        vigente: document.getElementById("chkImagenProductoVigente").checked ? 1 : 0
    };
}

function limpiarFormularioImagenProducto() {
    limpiarErrores();
    document.getElementById("txtIdImagenProducto").value = "";
    document.getElementById("ddlPlataformaImagen").value = "";
    document.getElementById("ddlTipoImagenProducto").value = "";
    document.getElementById("txtImagenProducto").value = "";
    document.getElementById("fileImagenProducto").value = "";
    document.getElementById("txtDescripcionImagenProducto").value = "";
    ocultarPreviewImagenProducto();
    inicializarToggleImagenProducto(true);
}

function mostrarPreviewImagenProducto() {
    const ruta = document.getElementById("txtImagenProducto").value.trim();
    const preview = document.getElementById("previewImagenProducto");
    if (!ruta) {
        ocultarPreviewImagenProducto();
        return;
    }

    preview.src = ruta;
    preview.classList.remove("d-none");
}

function mostrarPreviewImagenProductoDesdeArchivo() {
    const archivo = document.getElementById("fileImagenProducto")?.files?.[0];
    if (!archivo) {
        mostrarPreviewImagenProducto();
        return;
    }

    const preview = document.getElementById("previewImagenProducto");
    const reader = new FileReader();
    reader.onload = event => {
        preview.src = event.target.result;
        preview.classList.remove("d-none");
    };
    reader.onerror = () => ocultarPreviewImagenProducto();
    reader.readAsDataURL(archivo);
}

function ocultarPreviewImagenProducto() {
    const preview = document.getElementById("previewImagenProducto");
    preview.removeAttribute("src");
    preview.classList.add("d-none");
}

function formatearFecha(fecha) {
    return fecha ? new Date(fecha).toLocaleString() : "";
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

function inicializarToggleImagenProducto(activo) {
    setTimeout(() => {
        $("#chkImagenProductoVigente").bootstrapToggle("destroy");
        $("#chkImagenProductoVigente").bootstrapToggle({
            on: "Activo",
            off: "Inactivo",
            onstyle: "primary",
            offstyle: "danger",
            size: "small",
            width: 92,
            height: 30
        });
        $("#chkImagenProductoVigente").bootstrapToggle(activo ? "on" : "off");
    }, 50);
}
