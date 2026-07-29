let tablaCodigos;

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
    inicializarToggleCodigo(true);
    document.getElementById("BtnGenerarCodigo")?.addEventListener("click", P_GenerarCodigoCompra);
    document.getElementById("BtnNuevoCodigo")?.addEventListener("click", limpiarFormularioCodigo);
    document.getElementById("codigoCompraModalClose")?.addEventListener("click", cerrarModalCodigo);
    document.getElementById("codigoCompraModalCloseFooter")?.addEventListener("click", cerrarModalCodigo);
    document.getElementById("codigoCompraCopy")?.addEventListener("click", () => copiarTexto(document.getElementById("codigoCompraTexto")?.value || ""));
    F_GetCodigosCompraList();
});

function F_GetCodigosCompraList() {
    if (!tablaCodigos) {
        tablaCodigos = Grilla({
            tableSelector: "#tablaCodigos",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "codigo",
            columns: [
                { title: "Codigo", data: "codigo", width: 22 },
                { title: "Nombre cliente", data: "nombre_Cliente", width: 18 },
                { title: "Correo cliente", data: "correo_Cliente", width: 20 },
                { title: "Valor inicial", data: "valor_Inicial", width: 14, className: "text-end", render: valorMoneda },
                { title: "Saldo", data: "saldo_Disponible", width: 14, className: "text-end", render: valorMoneda },
                { title: "Expira", data: "fecha_Expiracion", width: 14, searchable: false, render: formatearFecha },
                { title: "Estado", data: "vigente", width: 10, className: "text-center", searchable: false, render: renderEstado }
            ],
            actions: {
                title: "Opciones",
                width: 12,
                items: [
                    { action: "ver", label: "Ver", icon: "fa-solid fa-eye", onClick: row => mostrarModalCodigo(row) },
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetCodigoCompra(row.id_Codigo_Compra) },
                    { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteCodigoCompra(row.id_Codigo_Compra) }
                ]
            }
        });
    }

    secureFetch("/api/CodigosCompraApi/F_GetCodigosCompraList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaCodigos.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de codigos."));
}

function P_GenerarCodigoCompra() {
    limpiarErrores();
    const idCodigo = document.getElementById("txtIdCodigoCompra").value;
    const esActualizacion = Boolean(idCodigo);
    const nombreCliente = document.getElementById("txtCodigoNombreCliente").value.trim();
    const correoCliente = document.getElementById("txtCodigoCorreoCliente").value.trim();
    const valor = Number(document.getElementById("txtCodigoValor").value || 0);
    const fecha = document.getElementById("txtCodigoExpiracion").value;

    if (!nombreCliente) { mostrarError("txtCodigoNombreCliente", "El nombre del cliente es obligatorio"); return; }
    if (!correoCliente) { mostrarError("txtCodigoCorreoCliente", "El correo del cliente es obligatorio"); return; }
    if (esActualizacion && valor < 0) { mostrarError("txtCodigoValor", "El saldo no puede ser negativo"); return; }
    if (!esActualizacion && valor <= 0) { mostrarError("txtCodigoValor", "El valor debe ser mayor a cero"); return; }

    const payload = {
        nombre_Cliente: nombreCliente,
        correo_Cliente: correoCliente,
        valor,
        fecha_Expiracion: fecha ? new Date(`${fecha}T23:59:59`).toISOString() : null,
        vigente: document.getElementById("chkCodigoVigente")?.checked ? 1 : 0
    };

    mostrarConfirmacion(esActualizacion ? "Actualizar codigo?" : "Generar codigo?", "Verifica que los datos sean correctos.", confirmado => {
        if (!confirmado) return;

        secureFetch(esActualizacion ? `/api/CodigosCompraApi/P_UdpCodigoCompra/${idCodigo}` : "/api/CodigosCompraApi/P_GenerarCodigoCompra", {
            method: esActualizacion ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarModalCodigo(data.data);
                    mostrarAlerta("exito", esActualizacion ? "Codigo actualizado" : "Codigo generado", data.mensaje);
                    limpiarFormularioCodigo();
                    F_GetCodigosCompraList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo generar el codigo."));
    });
}

function F_GetCodigoCompra(idCodigo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    secureFetch(`/api/CodigosCompraApi/F_GetCodigoCompra/${idCodigo}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data?.ok) {
                mostrarAlerta("error", "Error", data?.mensaje || "No se pudo consultar el codigo.");
                return;
            }

            const codigo = data.data;
            document.getElementById("txtIdCodigoCompra").value = obtenerValor(codigo, "id_Codigo_Compra", "idCodigoCompra", "Id_Codigo_Compra", "IdCodigoCompra");
            document.getElementById("txtCodigoNombreCliente").value = obtenerValor(codigo, "nombre_Cliente", "nombreCliente", "Nombre_Cliente", "NombreCliente") || "";
            document.getElementById("txtCodigoCorreoCliente").value = obtenerValor(codigo, "correo_Cliente", "correoCliente", "Correo_Cliente", "CorreoCliente") || "";
            document.getElementById("txtCodigoValor").value = Number(obtenerValor(codigo, "saldo_Disponible", "saldoDisponible", "Saldo_Disponible", "SaldoDisponible") || 0);
            document.getElementById("txtCodigoExpiracion").value = fechaInput(obtenerValor(codigo, "fecha_Expiracion", "fechaExpiracion", "Fecha_Expiracion", "FechaExpiracion"));
            inicializarToggleCodigo(obtenerValor(codigo, "vigente", "Vigente") == 1);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar el codigo."));
}

function P_DeleteCodigoCompra(idCodigo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    mostrarConfirmacion("Eliminar codigo?", "Esta accion marcara el codigo como inactivo.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/CodigosCompraApi/P_DeleteCodigoCompra/${idCodigo}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Codigo eliminado", data.mensaje);
                    F_GetCodigosCompraList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el codigo."));
    });
}

function mostrarModalCodigo(item) {
    const texto = formatearCodigoCompra(item);
    const body = document.getElementById("codigoCompraTexto");
    const modal = document.getElementById("codigoCompraModal");
    if (!body || !modal) {
        copiarTexto(texto);
        return;
    }

    body.value = texto;
    modal.style.display = "flex";
}

function cerrarModalCodigo() {
    const modal = document.getElementById("codigoCompraModal");
    if (!modal) return;
    modal.style.display = "none";
    const body = document.getElementById("codigoCompraTexto");
    if (body) body.value = "";
}

function formatearCodigoCompra(item) {
    const codigo = obtenerValor(item, "codigo", "Codigo") || "";
    const cliente = obtenerValor(item, "nombre_Cliente", "nombreCliente", "Nombre_Cliente", "NombreCliente") || "";
    const saldo = obtenerValor(item, "saldo_Disponible", "saldoDisponible", "Saldo_Disponible", "SaldoDisponible", "valor_Inicial", "valorInicial", "Valor_Inicial", "ValorInicial") || 0;

    return `**Codigo:** ${codigo}
**Cliente:** ${cliente}
**Saldo:** ${valorMoneda(saldo)}`;
}

function obtenerValor(item, ...keys) {
    if (!item) return "";
    for (const key of keys) {
        if (Object.prototype.hasOwnProperty.call(item, key) && item[key] !== null && item[key] !== undefined) {
            return item[key];
        }
    }
    return "";
}

function copiarTexto(texto) {
    navigator.clipboard?.writeText(texto)
        .then(() => mostrarAlerta("exito", "Copiado", "Informacion copiada al portapapeles."))
        .catch(() => mostrarAlerta("advertencia", "No se pudo copiar", texto));
}

function limpiarFormularioCodigo() {
    limpiarErrores();
    document.getElementById("txtIdCodigoCompra").value = "";
    document.getElementById("txtCodigoNombreCliente").value = "";
    document.getElementById("txtCodigoCorreoCliente").value = "";
    document.getElementById("txtCodigoValor").value = "";
    document.getElementById("txtCodigoExpiracion").value = "";
    inicializarToggleCodigo(true);
}

function valorMoneda(valor) {
    return `$${Number(valor || 0).toLocaleString()}`;
}

function formatearFecha(fecha) {
    return fecha ? new Date(fecha).toLocaleDateString() : "";
}

function fechaInput(fecha) {
    if (!fecha) return "";
    return new Date(fecha).toISOString().slice(0, 10);
}

function inicializarToggleCodigo(activo) {
    setTimeout(() => {
        $("#chkCodigoVigente").bootstrapToggle("destroy");
        $("#chkCodigoVigente").bootstrapToggle({
            on: "Activo",
            off: "Inactivo",
            onstyle: "primary",
            offstyle: "danger",
            size: "small",
            width: 92,
            height: 30
        });
        $("#chkCodigoVigente").bootstrapToggle(activo ? "on" : "off");
    }, 50);
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}
