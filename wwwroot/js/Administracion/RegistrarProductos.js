let tablaCuentas;

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
    inicializarToggleCuenta(true);
    document.getElementById("txtTiempoPantalla").value = 30;
    document.getElementById("BtnGuardarCuenta")?.addEventListener("click", P_SaveCuenta);
    document.getElementById("BtnNuevoCuenta")?.addEventListener("click", limpiarFormularioCuenta);
    F_GetCuentasList();
});

function F_GetCuentasList() {
    if (!tablaCuentas) {
        tablaCuentas = Grilla({
            tableSelector: "#tablaCuentas",
            search: true,
            sorting: true,
            pagination: true,
            pageSize: true,
            info: true,
            defaultPageSize: 5,
            pageSizeOptions: [5, 10, 20, "all"],
            defaultSortKey: "plataforma",
            columns: [
                { title: "Plataforma", data: "plataforma", width: 12 },
                { title: "Tipo", data: "tipoUsuario", width: 10 },
                { title: "Dias", data: "tiempo_Pantalla", width: 6, className: "text-center", searchable: false },
                { title: "Correo", data: "correo_Cuenta", width: 18 },
                { title: "Perfil", data: "perfil_Cuenta", width: 10 },
                { title: "Estado", data: "vigente", width: 8, className: "text-center", searchable: true, render: renderEstado, searchValue: vigente => vigente == 1 ? "Activo" : "Inactivo" }
            ],
            actions: {
                title: "Opciones",
                width: 10,
                items: [
                    { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetCuenta(row.id_Cuenta) },
                    { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteCuenta(row.id_Cuenta) }
                ]
            }
        });
    }

    secureFetch("/api/RegistrarProductosApi/F_GetCuentasList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaCuentas.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de productos."));
}

function P_SaveCuenta() {
    limpiarErrores();
    const idCuenta = document.getElementById("txtIdCuenta").value;
    const payload = obtenerPayloadCuenta();
    if (!payload) return;

    const esActualizacion = Boolean(idCuenta);
    mostrarConfirmacion(esActualizacion ? "Actualizar producto?" : "Registrar producto?", "Verifica que los datos sean correctos.", confirmado => {
        if (!confirmado) return;

        secureFetch(esActualizacion ? `/api/RegistrarProductosApi/P_UdpCuenta/${idCuenta}` : "/api/RegistrarProductosApi/P_InsCuenta", {
            method: esActualizacion ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;

                if (data.ok) {
                    mostrarAlerta("exito", esActualizacion ? "Actualizado" : "Registrado", data.mensaje);
                    limpiarFormularioCuenta();
                    F_GetCuentasList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el producto."));
    });
}

function F_GetCuenta(idCuenta) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    secureFetch(`/api/RegistrarProductosApi/F_GetCuenta/${idCuenta}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data?.ok) return;
            const item = data.data;
            document.getElementById("txtIdCuenta").value = item.id_Cuenta;
            document.getElementById("ddlPlataforma").value = item.id_Plataforma;
            document.getElementById("ddlTipoUsuario").value = item.id_Tipo_Usuario;
            document.getElementById("txtTiempoPantalla").value = item.tiempo_Pantalla || 30;
            document.getElementById("txtCorreoCuenta").value = item.correo_Cuenta || "";
            document.getElementById("txtContrasenaCuenta").value = item.contrasena_Cuenta || "";
            document.getElementById("txtPerfilCuenta").value = item.perfil_Cuenta || "";
            document.getElementById("txtPinCuenta").value = item.pin_Cuenta || "";
            inicializarToggleCuenta(item.vigente == 1);
            configurarBotonCuenta(true);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el producto."));
}

function P_DeleteCuenta(idCuenta) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");

    mostrarConfirmacion("Eliminar producto?", "Esta accion marcara el producto como inactivo.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/RegistrarProductosApi/P_DeleteCuenta/${idCuenta}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Producto eliminado", data.mensaje);
                    F_GetCuentasList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el producto."));
    });
}

function obtenerPayloadCuenta() {
    const idPlataforma = Number(document.getElementById("ddlPlataforma").value || 0);
    const idTipoUsuario = Number(document.getElementById("ddlTipoUsuario").value || 0);
    const tiempoPantalla = Number(document.getElementById("txtTiempoPantalla").value || 0);
    const correo = document.getElementById("txtCorreoCuenta").value.trim();
    const contrasena = document.getElementById("txtContrasenaCuenta").value.trim();

    if (!idPlataforma) { mostrarError("ddlPlataforma", "La plataforma es obligatoria"); return null; }
    if (!idTipoUsuario) { mostrarError("ddlTipoUsuario", "El tipo de usuario es obligatorio"); return null; }
    if (tiempoPantalla <= 0) { mostrarError("txtTiempoPantalla", "El tiempo de pantalla debe ser mayor a cero"); return null; }
    if (!correo) { mostrarError("txtCorreoCuenta", "El correo es obligatorio"); return null; }
    if (!contrasena) { mostrarError("txtContrasenaCuenta", "La contrasena es obligatoria"); return null; }

    return {
        id_Plataforma: idPlataforma,
        id_Tipo_Usuario: idTipoUsuario,
        tiempo_Pantalla: tiempoPantalla,
        correo_Cuenta: correo,
        contrasena_Cuenta: contrasena,
        perfil_Cuenta: valorCampo("txtPerfilCuenta"),
        pin_Cuenta: valorCampo("txtPinCuenta"),
        vigente: document.getElementById("chkCuentaVigente").checked ? 1 : 0
    };
}

function limpiarFormularioCuenta() {
    limpiarErrores();
    document.getElementById("txtIdCuenta").value = "";
    document.getElementById("ddlPlataforma").value = "";
    document.getElementById("ddlTipoUsuario").value = "";
    document.getElementById("txtTiempoPantalla").value = 30;
    document.getElementById("txtCorreoCuenta").value = "";
    document.getElementById("txtContrasenaCuenta").value = "";
    document.getElementById("txtPerfilCuenta").value = "";
    document.getElementById("txtPinCuenta").value = "";
    inicializarToggleCuenta(true);
    configurarBotonCuenta(false);
}

function configurarBotonCuenta(esActualizacion) {
    const boton = document.getElementById("BtnGuardarCuenta");
    boton.innerHTML = esActualizacion
        ? '<i class="fa-solid fa-pen-to-square me-2"></i> Actualizar'
        : '<i class="fa-solid fa-save me-2"></i> Guardar';
}

function valorCampo(id) {
    return document.getElementById(id).value.trim() || null;
}

function renderEstado(vigente) {
    const badge = document.createElement("span");
    badge.className = vigente == 1 ? "table-status table-status-active" : "table-status table-status-inactive";
    badge.textContent = vigente == 1 ? "Activo" : "Inactivo";
    return badge;
}

function inicializarToggleCuenta(activo) {
    setTimeout(() => {
        $("#chkCuentaVigente").bootstrapToggle("destroy");
        $("#chkCuentaVigente").bootstrapToggle({
            on: "Activo",
            off: "Inactivo",
            onstyle: "primary",
            offstyle: "danger",
            size: "small",
            width: 92,
            height: 30
        });
        $("#chkCuentaVigente").bootstrapToggle(activo ? "on" : "off");
    }, 50);
}

