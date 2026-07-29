let tablaBilleteras;

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
    inicializarToggleBilletera(true);
    document.getElementById("BtnRecargarBilletera")?.addEventListener("click", P_RecargarBilletera);
    document.getElementById("BtnNuevaRecarga")?.addEventListener("click", limpiarFormularioBilletera);
    F_GetBilleterasList();
});

function F_GetBilleterasList() {
    if (!tablaBilleteras) {
        tablaBilleteras = Grilla({
            tableSelector: "#tablaBilleteras",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "nombre",
            columns: [
                { title: "Nombre", data: "nombre", width: 30 },
                { title: "Usuario", data: "usuario", width: 20 },
                { title: "Saldo", data: "saldo", width: 18, className: "text-end", render: valor => `$${Number(valor || 0).toLocaleString()}` },
                { title: "Estado", data: "vigente", width: 12, className: "text-center", searchable: false, render: renderEstado }
            ],
            actions: {
                title: "Opciones",
                width: 12,
                items: [
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetBilletera(row.id_Billetera) }
                ]
            }
        });
    }

    secureFetch("/api/BilleteraVendedoresApi/F_GetBilleterasList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaBilleteras.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de billeteras."));
}

function P_RecargarBilletera() {
    limpiarErrores();
    const idBilletera = document.getElementById("txtIdBilletera").value;
    const esActualizacion = Boolean(idBilletera);
    const idUsuario = Number(document.getElementById("ddlBilleteraUsuario").value || 0);
    const valor = Number(document.getElementById("txtBilleteraValor").value || 0);

    if (!idUsuario && !esActualizacion) { mostrarError("ddlBilleteraUsuario", "El vendedor es obligatorio"); return; }
    if (esActualizacion && valor < 0) { mostrarError("txtBilleteraValor", "El saldo no puede ser negativo"); return; }
    if (!esActualizacion && valor <= 0) { mostrarError("txtBilleteraValor", "El valor debe ser mayor a cero"); return; }

    const payload = esActualizacion
        ? {
            saldo: valor,
            descripcion: document.getElementById("txtBilleteraDescripcion").value.trim() || null,
            vigente: document.getElementById("chkBilleteraVigente")?.checked ? 1 : 0
        }
        : {
            id_Usuario: idUsuario,
            valor,
            descripcion: document.getElementById("txtBilleteraDescripcion").value.trim() || null
        };

    mostrarConfirmacion(esActualizacion ? "Actualizar billetera?" : "Recargar billetera?", "Verifica que el valor sea correcto.", confirmado => {
        if (!confirmado) return;

        secureFetch(esActualizacion ? `/api/BilleteraVendedoresApi/P_UdpBilletera/${idBilletera}` : "/api/BilleteraVendedoresApi/P_RecargarBilletera", {
            method: esActualizacion ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", esActualizacion ? "Billetera actualizada" : "Recarga realizada", data.mensaje);
                    limpiarFormularioBilletera();
                    F_GetBilleterasList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo recargar la billetera."));
    });
}

function F_GetBilletera(idBilletera) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    secureFetch(`/api/BilleteraVendedoresApi/F_GetBilletera/${idBilletera}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data?.ok) {
                mostrarAlerta("error", "Error", data?.mensaje || "No se pudo consultar la billetera.");
                return;
            }

            const billetera = data.data;
            document.getElementById("txtIdBilletera").value = billetera.id_Billetera;
            document.getElementById("ddlBilleteraUsuario").value = billetera.id_Usuario;
            document.getElementById("ddlBilleteraUsuario").dispatchEvent(new Event("change"));
            document.getElementById("ddlBilleteraUsuario").disabled = true;
            document.getElementById("txtBilleteraValor").value = Number(billetera.saldo || 0);
            document.getElementById("txtBilleteraDescripcion").value = "";
            inicializarToggleBilletera(billetera.vigente == 1);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo consultar la billetera."));
}

function limpiarFormularioBilletera() {
    limpiarErrores();
    document.getElementById("txtIdBilletera").value = "";
    document.getElementById("ddlBilleteraUsuario").value = "";
    document.getElementById("ddlBilleteraUsuario").disabled = false;
    document.getElementById("ddlBilleteraUsuario").dispatchEvent(new Event("change"));
    document.getElementById("txtBilleteraValor").value = "";
    document.getElementById("txtBilleteraDescripcion").value = "";
    inicializarToggleBilletera(true);
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

function inicializarToggleBilletera(activo) {
    setTimeout(() => {
        $("#chkBilleteraVigente").bootstrapToggle("destroy");
        $("#chkBilleteraVigente").bootstrapToggle({
            on: "Activo",
            off: "Inactivo",
            onstyle: "primary",
            offstyle: "danger",
            size: "small",
            width: 92,
            height: 30
        });
        $("#chkBilleteraVigente").bootstrapToggle(activo ? "on" : "off");
    }, 50);
}
