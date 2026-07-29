let tablaCorreosPlataformas;

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
    configurarTablaCorreos();
    F_GetCorreosPlataformasList();
    document.getElementById("BtnSincronizarCorreos")?.addEventListener("click", P_SincronizarCorreos);
    document.getElementById("btnCerrarDetalleCorreoPlataforma")?.addEventListener("click", cerrarDetalleCorreo);
    document.getElementById("btnCerrarDetalleCorreoPlataformaFooter")?.addEventListener("click", cerrarDetalleCorreo);
});

function configurarTablaCorreos() {
    tablaCorreosPlataformas = Grilla({
        tableSelector: "#tablaCorreosPlataformas",
        search: true,
        sorting: true,
        pagination: true,
        pageSize: true,
        info: true,
        defaultPageSize: 10,
        pageSizeOptions: [5, 10, 20, "all"],
        defaultSortKey: "fecha_Recepcion",
        columns: [
            { title: "Remitente", data: "remitente", width: 20 },
            { title: "Destinatarios", data: "destinatarios", width: 24 },
            { title: "Asunto", data: "asunto", width: 28 },
            { title: "Fecha recepcion", data: "fecha_Recepcion", width: 16, searchable: false, render: formatearFecha }
        ],
        actions: {
            title: "Opciones",
            width: 12,
            items: [
                { action: "ver", label: "Ver correo", icon: "fa-solid fa-eye", onClick: row => F_GetCorreoDetalle(row.id_Correo_Plataforma) },
                { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteCorreo(row.id_Correo_Plataforma) }
            ]
        }
    });
}

function F_GetCorreosPlataformasList() {
    secureFetch("/api/CodigosPlataformasApi/F_GetCorreosList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaCorreosPlataformas.setData(data.data || []);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de correos."));
}

function P_SincronizarCorreos() {
    mostrarConfirmacion("Sincronizar correos?", "Se consultara el buzon central configurado.", confirmado => {
        if (!confirmado) return;

        secureFetch("/api/CodigosPlataformasApi/P_SincronizarBuzon", { method: "POST" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Sincronizacion completada", data.mensaje);
                    F_GetCorreosPlataformasList();
                    return;
                }

                mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo sincronizar el buzon."));
    });
}

function F_GetCorreoDetalle(idCorreo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    secureFetch(`/api/CodigosPlataformasApi/F_GetCorreoDetalle/${idCorreo}`)
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) {
                mostrarDetalleCorreo(data.data);
                return;
            }

            if (data) mostrarAlerta("error", "Error", data.mensaje);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar el correo."));
}

function P_DeleteCorreo(idCorreo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    mostrarConfirmacion("Eliminar correo?", "Esta accion eliminara fisicamente el correo del sistema.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/CodigosPlataformasApi/P_DeleteCorreo/${idCorreo}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Correo eliminado", data.mensaje);
                    F_GetCorreosPlataformasList();
                    return;
                }

                mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el correo."));
    });
}

function mostrarDetalleCorreo(correo) {
    document.getElementById("tituloDetalleCorreoPlataforma").textContent = correo.asunto || "Detalle correo";
    document.getElementById("detalleCorreoPlataformaMeta").innerHTML = construirMeta(correo);
    document.getElementById("detalleCorreoPlataformaLinks").innerHTML = construirLinks(correo.enlaces || []);
    document.getElementById("detalleCorreoPlataformaFrame").srcdoc = construirDocumentoCorreo(correo.cuerpo_Html || correo.cuerpo_Texto || "");
    document.getElementById("modalDetalleCorreoPlataforma").style.display = "flex";
}

function cerrarDetalleCorreo() {
    document.getElementById("modalDetalleCorreoPlataforma").style.display = "none";
    document.getElementById("detalleCorreoPlataformaFrame").srcdoc = "";
}

function construirMeta(correo) {
    return `
        <p><strong>De:</strong> ${escapeHtml(correo.remitente || "")}</p>
        <p><strong>Para:</strong> ${escapeHtml(correo.destinatarios || "")}</p>
        <p><strong>Fecha:</strong> ${escapeHtml(formatearFecha(correo.fecha_Recepcion))}</p>
    `;
}

function construirLinks(enlaces) {
    if (!enlaces.length) return "";
    return enlaces.map((enlace, index) => `
        <a href="${escapeAttr(enlace.url)}" target="_blank" rel="noopener noreferrer" class="btn btn-outline-light btn-sm">
            <i class="fa-solid fa-arrow-up-right-from-square me-1"></i> ${escapeHtml(enlace.texto || `Abrir enlace ${index + 1}`)}
        </a>
    `).join("");
}

function construirDocumentoCorreo(html) {
    return `<!doctype html><html><head><base target="_blank"><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data: blob:; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; script-src 'none';"><style>body{font-family:Arial,sans-serif;margin:16px;color:#111;background:#fff;}img{max-width:100%;height:auto;}a{color:#0d6efd;}</style></head><body>${html}</body></html>`;
}

function formatearFecha(fecha) {
    return fecha ? new Date(fecha).toLocaleString() : "";
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, ch => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch]));
}

function escapeAttr(value) {
    return escapeHtml(value).replace(/`/g, "&#96;");
}

