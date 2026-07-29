let tablaCombos;
let tablaComboPlataformas;
let plataformasCombo = [];

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
    document.getElementById("txtComboTiempoPantalla").value = 30;
    document.getElementById("txtComboOrden").value = 1;
    document.getElementById("txtComboPlataformaCantidad").value = 1;
    document.getElementById("BtnAgregarPlataformaCombo")?.addEventListener("click", agregarPlataformaCombo);
    document.getElementById("BtnGuardarCombo")?.addEventListener("click", P_SaveCombo);
    document.getElementById("BtnNuevoCombo")?.addEventListener("click", limpiarFormularioCombo);
    document.getElementById("btnSubirImagenCombo")?.addEventListener("click", P_UploadImagenCombo);
    document.getElementById("fileImagenCombo")?.addEventListener("change", mostrarPreviewImagenComboDesdeArchivo);
    inicializarTablas();
    F_GetCombosList();
});

function inicializarTablas() {
    tablaComboPlataformas = Grilla({
        tableSelector: "#tablaComboPlataformas",
        search: false,
        sorting: false,
        pagination: false,
        pageSize: false,
        info: false,
        columns: [
            { title: "Plataforma", data: "plataforma", width: 70 },
            { title: "Cantidad", data: "cantidad", width: 20, className: "text-center" }
        ],
        actions: {
            title: "Opciones",
            width: 10,
            items: [
                { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => quitarPlataformaCombo(row.id_Plataforma) }
            ]
        }
    });

    tablaCombos = Grilla({
        tableSelector: "#tablaCombos",
        search: true,
        sorting: true,
        pagination: true,
        pageSize: true,
        info: true,
        defaultPageSize: 5,
        pageSizeOptions: [5, 10, 20, "all"],
        defaultSortKey: "orden",
        columns: [
            { title: "Orden", data: "orden", width: 6, className: "text-center", searchable: false },
            { title: "Nombre", data: "nombre", width: 18 },
            { title: "Tipo", data: "tipoUsuario", width: 12 },
            { title: "Dias", data: "tiempo_Pantalla", width: 8, className: "text-center", searchable: false },
            { title: "Precio", data: "precio", width: 10, className: "text-end", render: valor => `$${Number(valor || 0).toLocaleString()}` },
            { title: "Disponibles", data: "cantidadDisponible", width: 10, className: "text-center", searchable: false },
            { title: "Estado", data: "vigente", width: 10, className: "text-center", searchable: false, render: renderEstado }
        ],
        actions: {
            title: "Opciones",
            width: 10,
            items: [
                { action: "actualizar", label: "Actualizar", icon: "fa-solid fa-pen-to-square", onClick: row => F_GetCombo(row.id_Combo) },
                { action: "eliminar", label: "Eliminar", icon: "fa-solid fa-trash", onClick: row => P_DeleteCombo(row.id_Combo) }
            ]
        }
    });
}

function F_GetCombosList() {
    secureFetch("/api/RegistrarCombosApi/F_GetCombosList")
        .then(parseJsonResponse)
        .then(data => {
            if (data?.ok) tablaCombos.setData(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar la lista de combos."));
}

function F_GetCombo(idCombo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    secureFetch(`/api/RegistrarCombosApi/F_GetCombo/${idCombo}`)
        .then(parseJsonResponse)
        .then(data => {
            if (!data?.ok) return;
            const item = data.data;
            document.getElementById("txtIdCombo").value = item.id_Combo;
            document.getElementById("txtComboNombre").value = item.nombre || "";
            document.getElementById("ddlComboTipoUsuario").value = item.id_Tipo_Usuario;
            document.getElementById("txtComboTiempoPantalla").value = item.tiempo_Pantalla || 30;
            document.getElementById("txtComboPrecio").value = item.precio || "";
            document.getElementById("txtComboOrden").value = item.orden || 1;
            document.getElementById("txtComboImagenUrl").value = item.imagenUrl || "";
            document.getElementById("txtComboDescripcion").value = item.descripcion || "";
            mostrarPreviewImagenCombo();
            plataformasCombo = (item.plataformas || []).map(p => ({
                id_Plataforma: p.id_Plataforma,
                plataforma: p.plataforma,
                cantidad: p.cantidad
            }));
            renderPlataformasCombo();
            configurarBotonCombo(true);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar el combo."));
}

function agregarPlataformaCombo() {
    const ddl = document.getElementById("ddlComboPlataforma");
    const idPlataforma = Number(ddl.value || 0);
    const cantidad = Number(document.getElementById("txtComboPlataformaCantidad").value || 0);

    if (!idPlataforma) { mostrarError("ddlComboPlataforma", "La plataforma es obligatoria"); return; }
    if (cantidad <= 0) { mostrarError("txtComboPlataformaCantidad", "La cantidad debe ser mayor a cero"); return; }

    const existente = plataformasCombo.find(p => p.id_Plataforma === idPlataforma);
    if (existente) {
        existente.cantidad += cantidad;
    } else {
        plataformasCombo.push({
            id_Plataforma: idPlataforma,
            plataforma: ddl.options[ddl.selectedIndex].text,
            cantidad
        });
    }

    document.getElementById("ddlComboPlataforma").value = "";
    document.getElementById("txtComboPlataformaCantidad").value = 1;
    renderPlataformasCombo();
}

function quitarPlataformaCombo(idPlataforma) {
    plataformasCombo = plataformasCombo.filter(p => p.id_Plataforma !== idPlataforma);
    renderPlataformasCombo();
}

function renderPlataformasCombo() {
    tablaComboPlataformas.setData(plataformasCombo);
}

function P_SaveCombo() {
    limpiarErrores();
    const idCombo = document.getElementById("txtIdCombo").value;
    const payload = obtenerPayloadCombo();
    if (!payload) return;

    const esActualizacion = Boolean(idCombo);
    mostrarConfirmacion(esActualizacion ? "Actualizar combo?" : "Registrar combo?", "Verifica que los datos sean correctos.", confirmado => {
        if (!confirmado) return;

        subirImagenComboSiPendiente().then(imagenLista => {
            if (!imagenLista) return null;

            payload.imagenUrl = document.getElementById("txtComboImagenUrl").value.trim() || null;
            return secureFetch(esActualizacion ? `/api/RegistrarCombosApi/P_UdpCombo/${idCombo}` : "/api/RegistrarCombosApi/P_InsCombo", {
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
                    limpiarFormularioCombo();
                    F_GetCombosList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar el combo."));
    });
}

function P_UploadImagenCombo() {
    subirImagenComboSiPendiente(true);
}

function subirImagenComboSiPendiente(mostrarExito = false) {
    const inputArchivo = document.getElementById("fileImagenCombo");
    const archivo = inputArchivo?.files?.[0];

    if (!archivo) {
        return Promise.resolve(true);
    }

    const formData = new FormData();
    formData.append("imagen", archivo);

    return secureFetch("/api/RegistrarCombosApi/P_UploadImagenCombo", {
        method: "POST",
        body: formData
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return false;
            if (data.ok) {
                document.getElementById("txtComboImagenUrl").value = data.data;
                inputArchivo.value = "";
                mostrarPreviewImagenCombo();
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

function P_DeleteCombo(idCombo) {
    document.querySelectorAll(".table-menu").forEach(m => m.style.display = "none");
    mostrarConfirmacion("Eliminar combo?", "Esta accion marcara el combo como inactivo.", confirmado => {
        if (!confirmado) return;

        secureFetch(`/api/RegistrarCombosApi/P_DeleteCombo/${idCombo}`, { method: "DELETE" })
            .then(parseJsonResponse)
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    mostrarAlerta("exito", "Combo eliminado", data.mensaje);
                    F_GetCombosList();
                } else {
                    mostrarAlerta("error", "Error", data.mensaje);
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo eliminar el combo."));
    });
}

function obtenerPayloadCombo() {
    const nombre = document.getElementById("txtComboNombre").value.trim();
    const idTipoUsuario = Number(document.getElementById("ddlComboTipoUsuario").value || 0);
    const tiempoPantalla = Number(document.getElementById("txtComboTiempoPantalla").value || 0);
    const precio = Number(document.getElementById("txtComboPrecio").value || 0);
    const orden = Number(document.getElementById("txtComboOrden").value || 0);

    if (!nombre) { mostrarError("txtComboNombre", "El nombre es obligatorio"); return null; }
    if (!idTipoUsuario) { mostrarError("ddlComboTipoUsuario", "El tipo de usuario es obligatorio"); return null; }
    if (tiempoPantalla <= 0) { mostrarError("txtComboTiempoPantalla", "Los dias deben ser mayores a cero"); return null; }
    if (precio <= 0) { mostrarError("txtComboPrecio", "El precio debe ser mayor a cero"); return null; }
    if (orden <= 0) { mostrarError("txtComboOrden", "El orden debe ser mayor a cero"); return null; }
    if (!plataformasCombo.length) {
        mostrarAlerta("advertencia", "Datos incompletos", "Agrega al menos una plataforma al combo.");
        return null;
    }

    return {
        nombre,
        descripcion: document.getElementById("txtComboDescripcion").value.trim() || null,
        imagenUrl: document.getElementById("txtComboImagenUrl").value.trim() || null,
        id_Tipo_Usuario: idTipoUsuario,
        tiempo_Pantalla: tiempoPantalla,
        precio,
        orden,
        vigente: 1,
        plataformas: plataformasCombo.map(p => ({
            id_Plataforma: p.id_Plataforma,
            cantidad: p.cantidad
        }))
    };
}

function limpiarFormularioCombo() {
    limpiarErrores();
    document.getElementById("txtIdCombo").value = "";
    document.getElementById("txtComboNombre").value = "";
    document.getElementById("ddlComboTipoUsuario").value = "";
    document.getElementById("txtComboTiempoPantalla").value = 30;
    document.getElementById("txtComboPrecio").value = "";
    document.getElementById("txtComboOrden").value = 1;
    document.getElementById("txtComboImagenUrl").value = "";
    document.getElementById("fileImagenCombo").value = "";
    document.getElementById("txtComboDescripcion").value = "";
    ocultarPreviewImagenCombo();
    plataformasCombo = [];
    renderPlataformasCombo();
    configurarBotonCombo(false);
}

function mostrarPreviewImagenComboDesdeArchivo() {
    const archivo = document.getElementById("fileImagenCombo")?.files?.[0];
    if (!archivo) {
        mostrarPreviewImagenCombo();
        return;
    }

    const preview = document.getElementById("previewImagenCombo");
    const reader = new FileReader();
    reader.onload = event => {
        preview.src = event.target.result;
        preview.classList.remove("d-none");
    };
    reader.onerror = () => ocultarPreviewImagenCombo();
    reader.readAsDataURL(archivo);
}

function mostrarPreviewImagenCombo() {
    const ruta = document.getElementById("txtComboImagenUrl").value.trim();
    const preview = document.getElementById("previewImagenCombo");
    if (!ruta) {
        ocultarPreviewImagenCombo();
        return;
    }

    preview.src = ruta;
    preview.classList.remove("d-none");
}

function ocultarPreviewImagenCombo() {
    const preview = document.getElementById("previewImagenCombo");
    preview.removeAttribute("src");
    preview.classList.add("d-none");
}

function configurarBotonCombo(esActualizacion) {
    const boton = document.getElementById("BtnGuardarCombo");
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
