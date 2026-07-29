let tablaPrecios;

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
    inicializarTogglePrecio(true);
    document.getElementById("txtPrecioTiempoPantalla").value = 30;
    document.getElementById("BtnGuardarPrecio")?.addEventListener("click", P_SavePrecio);
    document.getElementById("BtnNuevoPrecio")?.addEventListener("click", limpiarFormularioPrecio);
    F_GetPreciosProductoList();
});

function F_GetPreciosProductoList() {
    if (!tablaPrecios) {
        tablaPrecios = Grilla({
            tableSelector: "#tablaPrecios",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "plataforma",
            columns: [
                { title: "Plataforma", data: "plataforma", width: 20 },
                { title: "Tipo", data: "tipoUsuario", width: 16 },
                { title: "Dias", data: "tiempo_Pantalla", width: 10, className: "text-center", searchable: false },
                { title: "Precio", data: "precio", width: 14, className: "text-end", render: valor => `$${Number(valor || 0).toLocaleString()}` },
                { title: "Estado", data: "vigente", width: 10, className: "text-center", searchable: false, render: renderEstado }
            ],
            actions: {
                title: "Opciones",
                width: 10,
                items: [
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => cargarPrecio(row) },
                    { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeletePrecio(row.id_Precio_Producto) }
                ]
            }
        });
    }

    secureFetch("/api/RegistrarPreciosApi/F_GetPreciosProductoList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaPrecios.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de precios."));
}

function cargarPrecio(item) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    document.getElementById("txtIdPrecioProducto").value = item.id_Precio_Producto;
    document.getElementById("ddlPrecioPlataforma").value = item.id_Plataforma;
    document.getElementById("ddlPrecioTipoUsuario").value = item.id_Tipo_Usuario;
    document.getElementById("txtPrecioTiempoPantalla").value = item.tiempo_Pantalla;
    document.getElementById("txtPrecioValor").value = item.precio;
    inicializarTogglePrecio(item.vigente == 1);
    configurarBotonPrecio(true);
}

function P_SavePrecio() {
    limpiarErrores();
    const idPrecio = document.getElementById("txtIdPrecioProducto").value;
    const payload = obtenerPayloadPrecio();
    if (!payload) return;

    const esActualizacion = Boolean(idPrecio);
    mostrarConfirmacion(esActualizacion ? "Actualizar precio?" : "Registrar precio?", "Verifica que los datos sean correctos.", confirmado => {
        if (!confirmado) return;

        secureFetch(esActualizacion ? `/api/RegistrarPreciosApi/P_UdpPrecioProducto/${idPrecio}` : "/api/RegistrarPreciosApi/P_InsPrecioProducto", {
            method: esActualizacion ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Guardado", data.mensaje);
                    limpiarFormularioPrecio();
                    F_GetPreciosProductoList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el precio."));
    });
}

function P_DeletePrecio(idPrecio) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    mostrarConfirmacion("Eliminar precio?", "Esta accion marcara el precio como inactivo.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/RegistrarPreciosApi/P_DeletePrecioProducto/${idPrecio}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Precio eliminado", data.mensaje);
                    F_GetPreciosProductoList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el precio."));
    });
}

function obtenerPayloadPrecio() {
    const idPlataforma = Number(document.getElementById("ddlPrecioPlataforma").value || 0);
    const idTipoUsuario = Number(document.getElementById("ddlPrecioTipoUsuario").value || 0);
    const tiempoPantalla = Number(document.getElementById("txtPrecioTiempoPantalla").value || 0);
    const precio = Number(document.getElementById("txtPrecioValor").value || 0);

    if (!idPlataforma) { mostrarError("ddlPrecioPlataforma", "La plataforma es obligatoria"); return null; }
    if (!idTipoUsuario) { mostrarError("ddlPrecioTipoUsuario", "El tipo de usuario es obligatorio"); return null; }
    if (tiempoPantalla <= 0) { mostrarError("txtPrecioTiempoPantalla", "Los dias deben ser mayores a cero"); return null; }
    if (precio <= 0) { mostrarError("txtPrecioValor", "El precio debe ser mayor a cero"); return null; }

    return {
        id_Plataforma: idPlataforma,
        id_Tipo_Usuario: idTipoUsuario,
        tiempo_Pantalla: tiempoPantalla,
        precio,
        vigente: document.getElementById("chkPrecioVigente").checked ? 1 : 0
    };
}

function limpiarFormularioPrecio() {
    limpiarErrores();
    document.getElementById("txtIdPrecioProducto").value = "";
    document.getElementById("ddlPrecioPlataforma").value = "";
    document.getElementById("ddlPrecioTipoUsuario").value = "";
    document.getElementById("txtPrecioTiempoPantalla").value = 30;
    document.getElementById("txtPrecioValor").value = "";
    inicializarTogglePrecio(true);
    configurarBotonPrecio(false);
}

function configurarBotonPrecio(esActualizacion) {
    const boton = document.getElementById("BtnGuardarPrecio");
    boton.innerHTML = esActualizacion
        ? '<i class="fa-solid fa-pen-to-square me-2"></i> Actualizar'
        : '<i class="fa-solid fa-save me-2"></i> Guardar';
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

function inicializarTogglePrecio(activo) {
    setTimeout(() => {
        $("#chkPrecioVigente").bootstrapToggle("destroy");
        $("#chkPrecioVigente").bootstrapToggle({
            on: "Activo",
            off: "Inactivo",
            onstyle: "primary",
            offstyle: "danger",
            size: "small",
            width: 92,
            height: 30
        });
        $("#chkPrecioVigente").bootstrapToggle(activo ? "on" : "off");
    }, 50);
}
