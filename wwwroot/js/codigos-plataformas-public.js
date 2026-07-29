(function () {
    let tablaCodigosPlataformas;

    document.addEventListener("DOMContentLoaded", () => {
        configurarTabla();
        document.getElementById("btnBuscarCodigosPlataformas")?.addEventListener("click", buscarCorreos);
        document.getElementById("txtCorreoCodigoPlataforma")?.addEventListener("keydown", event => {
            if (event.key === "Enter") buscarCorreos();
        });
        document.getElementById("btnCerrarDetalleCorreoPublico")?.addEventListener("click", cerrarDetalle);
    });

    function configurarTabla() {
        tablaCodigosPlataformas = Grilla({
            tableSelector: "#tablaCodigosPlataformasPublica",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "fecha_Recepcion",
            columns: [
                { title: "Remitente", data: "remitente", width: 22 },
                { title: "Asunto", data: "asunto", width: 36 },
                { title: "Fecha", data: "fecha_Recepcion", width: 18, searchable: false, render: formatearFecha }
            ],
            actions: {
                title: "Opciones",
                width: 12,
                items: [
                    { action: "ver", label: "Ver correo", icon: "fa-solid fa-eye", onClick: row => consultarDetalle(row.id_Correo_Plataforma) }
                ]
            }
        });

        tablaCodigosPlataformas.setData([]);
    }

    function buscarCorreos() {
        const correo = obtenerCorreo();
        if (!correo) return;

        secureFetch("/api/CodigosPlataformasPublicApi/F_BuscarCorreos", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ correo })
        })
            .then(parseJsonResponse)
            .then(data => {
                if (data?.ok) {
                    tablaCodigosPlataformas.setData(data.data || []);
                    if (!(data.data || []).length) {
                        mostrarAlerta("advertencia", "Sin correos", "No hay correos recientes asociados a esa cuenta.");
                    }
                    return;
                }

                if (data) mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar la bandeja."));
    }

    function consultarDetalle(idCorreo) {
        const correo = obtenerCorreo();
        if (!correo) return;

        document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
        secureFetch("/api/CodigosPlataformasPublicApi/F_GetCorreoDetalle", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ correo, id_Correo_Plataforma: idCorreo })
        })
            .then(parseJsonResponse)
            .then(data => {
                if (data?.ok) {
                    mostrarDetalle(data.data);
                    return;
                }

                if (data) mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar el correo."));
    }

    function obtenerCorreo() {
        const correo = document.getElementById("txtCorreoCodigoPlataforma")?.value.trim();
        if (!correo) {
            mostrarAlerta("advertencia", "Correo requerido", "Ingresa el correo de la cuenta.");
            return null;
        }

        return correo;
    }

    function mostrarDetalle(correo) {
        document.getElementById("tituloDetalleCorreoPublico").textContent = correo.asunto || "Detalle correo";
        document.getElementById("detalleCorreoPublicoMeta").innerHTML = construirMeta(correo);
        document.getElementById("detalleCorreoPublicoLinks").innerHTML = construirLinks(correo.enlaces || []);
        document.getElementById("detalleCorreoPublicoFrame").srcdoc = construirDocumentoCorreo(correo.cuerpo_Html || correo.cuerpo_Texto || "");
        document.getElementById("modalDetalleCorreoPublico").classList.add("show");
    }

    function cerrarDetalle() {
        document.getElementById("modalDetalleCorreoPublico")?.classList.remove("show");
        document.getElementById("detalleCorreoPublicoFrame").srcdoc = "";
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

    function secureFetch(url, options = {}) {
        const method = (options.method || "GET").toUpperCase();
        const headers = new Headers(options.headers || {});
        if (method !== "GET" && method !== "HEAD" && method !== "OPTIONS") {
            headers.set("X-CSRF-TOKEN", document.getElementById("csrfToken")?.value || "");
        }

        return fetch(url, { ...options, headers, credentials: "same-origin" });
    }

    async function parseJsonResponse(response) {
        return await response.json().catch(() => null);
    }

    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, ch => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch]));
    }

    function escapeAttr(value) {
        return escapeHtml(value).replace(/`/g, "&#96;");
    }
})();

