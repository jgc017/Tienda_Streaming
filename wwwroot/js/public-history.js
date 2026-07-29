(function () {
    let tablaHistorialCliente;
    const formatter = new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    });

    document.addEventListener("DOMContentLoaded", () => {
        configurarTabla();
        document.getElementById("btnConsultarHistorialCliente")?.addEventListener("click", consultarHistorial);
        document.getElementById("publicHistoryDetailClose")?.addEventListener("click", cerrarDetalle);
        document.getElementById("publicHistoryDetailCopyAll")?.addEventListener("click", copiarDetalleCompleto);
    });

    function configurarTabla() {
        tablaHistorialCliente = Grilla({
            tableSelector: "#tablaHistorialCliente",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "fecha_Compra",
            columns: [
                { title: "Pedido", data: "id_Pedido", width: 10, className: "text-center", searchable: false },
                { title: "Nombre cliente", data: "nombre_Cliente", width: 18 },
                { title: "Correo", data: "correo_Cliente", width: 20 },
                { title: "Plataforma", data: "plataforma", width: 20 },
                { title: "Total", data: "total", width: 12, className: "text-end", render: valor => formatter.format(Number(valor || 0)) },
                { title: "Fecha", data: "fecha_Compra", width: 16, searchable: false, render: fecha => fecha ? new Date(fecha).toLocaleString() : "" }
            ],
            actions: {
                title: "Detalle",
                width: 12,
                items: [
                    { action: "detalle", label: "Ver Detalle Cuenta", icon: "fa-solid fa-eye", onClick: row => consultarDetalle(row.id_Pedido) }
                ]
            }
        });

        tablaHistorialCliente.setData([]);
    }

    function consultarHistorial() {
        const payload = obtenerCredenciales();
        if (!payload) return;

        secureFetch("/api/HistorialComprasClienteApi/F_GetHistorialComprasCliente", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(parseJsonResponse)
            .then(data => {
                if (data?.ok) {
                    tablaHistorialCliente.setData(data.data || []);
                    if (!(data.data || []).length) {
                        mostrarAlerta("advertencia", "Sin compras", "No hay compras asociadas a ese codigo.");
                    }
                    return;
                }

                if (data) mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar el historial."));
    }

    function consultarDetalle(idPedido) {
        const payload = obtenerCredenciales();
        if (!payload) return;

        document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
        secureFetch("/api/HistorialComprasClienteApi/F_GetDetalleCompraCliente", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ ...payload, id_Pedido: idPedido })
        })
            .then(parseJsonResponse)
            .then(data => {
                if (data?.ok) {
                    mostrarDetalle(data.data);
                    return;
                }

                if (data) mostrarAlerta("error", "Error", data.mensaje);
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar el detalle."));
    }

    function obtenerCredenciales() {
        const codigo = document.getElementById("txtHistorialCodigo")?.value.trim();
        const correo = document.getElementById("txtHistorialCorreo")?.value.trim();

        if (!codigo) {
            mostrarAlerta("advertencia", "Codigo requerido", "Ingresa el codigo de compra.");
            return null;
        }

        if (!correo) {
            mostrarAlerta("advertencia", "Correo requerido", "Ingresa el correo asociado al codigo.");
            return null;
        }

        return { codigo, correo_Cliente: correo };
    }

    function mostrarDetalle(data) {
        const body = document.getElementById("publicHistoryDetailBody");
        const modal = document.getElementById("publicHistoryDetailModal");
        if (!body || !modal) return;

        body.innerHTML = "";
        (data.cuentas || []).forEach((cuenta, index) => {
            const texto = formatearCuenta(cuenta, data.total, data.id_Pedido);
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

        modal.classList.add("show");
    }

    function formatearCuenta(cuenta, total, idPedido) {
        return `*# Pedido:* ${idPedido || ""}
*Cuenta:* ${cuenta.plataforma || ""}
*Correo:* ${cuenta.correo_Cuenta || ""}
*Contraseña:* ${cuenta.contrasena_Cuenta || ""}
*Perfil:* ${cuenta.perfil_Cuenta || ""}
*Pin:* ${cuenta.pin_Cuenta || ""}
*Fecha Vencimiento:* ${cuenta.fecha_Vencimiento ? new Date(cuenta.fecha_Vencimiento).toLocaleDateString() : ""}
*Total Pagado:* ${formatter.format(Number(total || 0))}`;
    }

    function cerrarDetalle() {
        document.getElementById("publicHistoryDetailModal")?.classList.remove("show");
    }

    function copiarDetalleCompleto() {
        const bloques = Array.from(document.querySelectorAll("#publicHistoryDetailBody pre")).map(p => p.textContent);
        copiarTexto(bloques.join("\n\n"));
    }

    function copiarTexto(texto) {
        navigator.clipboard?.writeText(texto)
            .then(() => mostrarAlerta("exito", "Copiado", "Informacion copiada al portapapeles."))
            .catch(() => mostrarAlerta("advertencia", "No se pudo copiar", texto));
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
        if (response.status === 403) {
            mostrarAlerta("advertencia", "Acceso denegado", "No tienes permisos para realizar esta accion.");
            return null;
        }

        return await response.json().catch(() => null);
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }
})();

