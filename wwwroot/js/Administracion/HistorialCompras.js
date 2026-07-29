let tablaHistorialCompras;

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
    document.getElementById("detalleCompraClose")?.addEventListener("click", cerrarDetalleCompra);
    document.getElementById("detalleCompraCloseFooter")?.addEventListener("click", cerrarDetalleCompra);
    document.getElementById("detalleCompraCopyAll")?.addEventListener("click", copiarDetalleCompleto);
    F_GetHistorialCompras();
});

function F_GetHistorialCompras() {
    if (!tablaHistorialCompras) {
        tablaHistorialCompras = Grilla({
            tableSelector: "#tablaHistorialCompras",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 10,
            pageSizeOptions: [10, 20, 50, "all"],
            defaultSortKey: "fecha_Compra",
            columns: [
                { title: "Pedido", data: "id_Pedido", width: 8, className: "text-center", searchable: false },
                { title: "Origen", data: "origen", width: 10 },
                { title: "Nombre cliente", data: "nombre_Cliente", width: 16 },
                { title: "Correo", data: "correo_Cliente", width: 18 },
                { title: "Plataforma", data: "plataforma", width: 18 },
                { title: "Tipo", data: "tipoUsuario", width: 12 },
                { title: "Usuario", data: "usuario", width: 16 },
                { title: "Codigo", data: "codigo", width: 14 },
                { title: "Cuentas", data: "cantidadCuentas", width: 8, className: "text-center", searchable: false },
                { title: "Total", data: "total", width: 12, className: "text-end", render: valor => `$${Number(valor || 0).toLocaleString()}` },
                { title: "Fecha", data: "fecha_Compra", width: 16, searchable: false, render: fecha => fecha ? new Date(fecha).toLocaleString() : "" }
            ],
            actions: {
                title: "Detalle",
                width: 10,
                items: [
                    { action: "detalle", label: "Ver Detalle Cuenta", icon: "fa-solid fa-eye", onClick: row => F_GetDetalleCompra(row.id_Pedido) }
                ]
            }
        });
    }

    secureFetch("/api/HistorialComprasApi/F_GetHistorialCompras")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaHistorialCompras.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el historial."));
}

function F_GetDetalleCompra(idPedido) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    secureFetch(`/api/HistorialComprasApi/F_GetDetalleCompra/${idPedido}`)
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) {
                mostrarDetalleCompra(data.data);
                return;
            }

            if (data) mostrarAlerta("error", "Error", data.mensaje);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el detalle de la compra."));
}

function mostrarDetalleCompra(data) {
    const body = document.getElementById("detalleCompraBody");
    const modal = document.getElementById("detalleCompraModal");
    if (!body || !modal) return;

    body.innerHTML = "";
    (data.cuentas || []).forEach((cuenta, index) => {
        const texto = formatearCuentaResultado(cuenta, data.total, data.id_Pedido);
        const block = document.createElement("article");
        block.className = "purchase-result-item";
        block.innerHTML = `
            <pre>${escapeHtml(texto)}</pre>
            <button type="button" class="btn btn-outline-light btn-sm">Copiar cuenta ${index + 1}</button>
        `;
        block.querySelector("button").addEventListener("click", () => copiarTexto(texto));
        body.appendChild(block);
    });

    if (!(data.cuentas || []).length) {
        body.innerHTML = '<p class="public-cart-empty">No hay cuentas asociadas a este pedido.</p>';
    }

    modal.style.display = "flex";
}

function formatearCuentaResultado(cuenta, total, idPedido) {
    return `*# Pedido:* ${idPedido || ""}
*Cuenta:* ${cuenta.plataforma || ""}
*Correo:* ${cuenta.correo_Cuenta || ""}
*Contraseña:* ${cuenta.contrasena_Cuenta || ""}
*Perfil:* ${cuenta.perfil_Cuenta || ""}
*Pin:* ${cuenta.pin_Cuenta || ""}
*Fecha Vencimiento:* ${cuenta.fecha_Vencimiento ? new Date(cuenta.fecha_Vencimiento).toLocaleDateString() : ""}
*Total Pagado:* ${valorMoneda(total)}`;
}

function copiarDetalleCompleto() {
    const bloques = Array.from(document.querySelectorAll("#detalleCompraBody pre")).map(p => p.textContent);
    copiarTexto(bloques.join("\n\n"));
}

function copiarTexto(texto) {
    navigator.clipboard?.writeText(texto)
        .then(() => mostrarAlerta("exito", "Copiado", "Informacion copiada al portapapeles."))
        .catch(() => mostrarAlerta("advertencia", "No se pudo copiar", texto));
}

function cerrarDetalleCompra() {
    const modal = document.getElementById("detalleCompraModal");
    if (modal) modal.style.display = "none";
}

function valorMoneda(valor) {
    return `$${Number(valor || 0).toLocaleString()}`;
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

