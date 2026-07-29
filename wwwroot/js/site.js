/*======================================================
=                 1. SUBMENÚ LATERAL                   =
======================================================*/

let solicitudesCargaActivas = 0;
let temporizadorCarga = null;

function asegurarOverlayCarga() {
    let overlay = document.getElementById("cargaSistema");
    if (overlay) return overlay;

    const logoSistema = document.querySelector("meta[name='app-logo']")?.content || "/img/IMAGENIA.png";

    overlay = document.createElement("div");
    overlay.id = "cargaSistema";
    overlay.className = "global-loading-overlay";
    overlay.setAttribute("role", "status");
    overlay.setAttribute("aria-live", "polite");
    overlay.setAttribute("aria-hidden", "true");
    overlay.innerHTML = `
        <div class="global-loading-card">
            <img src="${logoSistema}" class="global-loading-image" alt="" aria-hidden="true">
            <div class="global-loading-text" id="cargaSistemaTexto">Procesando...</div>
        </div>
    `;

    document.body.appendChild(overlay);
    return overlay;
}

function mostrarCargando(mensaje = "Procesando...") {
    const overlay = asegurarOverlayCarga();
    const texto = document.getElementById("cargaSistemaTexto");
    if (texto) texto.textContent = mensaje;

    solicitudesCargaActivas += 1;
    clearTimeout(temporizadorCarga);

    temporizadorCarga = setTimeout(() => {
        overlay.classList.add("is-visible");
        overlay.setAttribute("aria-hidden", "false");
        document.body.classList.add("global-loading-active");
    }, 120);
}

function ocultarCargando(forzar = false) {
    solicitudesCargaActivas = forzar ? 0 : Math.max(0, solicitudesCargaActivas - 1);

    if (solicitudesCargaActivas > 0) return;

    clearTimeout(temporizadorCarga);
    const overlay = asegurarOverlayCarga();
    overlay.classList.remove("is-visible");
    overlay.setAttribute("aria-hidden", "true");
    document.body.classList.remove("global-loading-active");
}

(function configurarCargaFetch() {
    if (!window.fetch || window.fetch.__plantillaLoader) return;

    const fetchOriginal = window.fetch.bind(window);

    const fetchConCarga = function (input, options = {}) {
        const mostrarLoader = options?.loader !== false;
        const mensaje = options?.loaderMessage || "Procesando solicitud...";

        if (mostrarLoader) {
            mostrarCargando(mensaje);
        }

        const opcionesLimpias = { ...options };
        delete opcionesLimpias.loader;
        delete opcionesLimpias.loaderMessage;

        return fetchOriginal(input, opcionesLimpias)
            .finally(() => {
                if (mostrarLoader) {
                    ocultarCargando();
                }
            });
    };

    fetchConCarga.__plantillaLoader = true;
    window.fetch = fetchConCarga;
})();

document.addEventListener("DOMContentLoaded", function () {
    asegurarOverlayCarga();
    inicializarSelectsBuscables();

    document.addEventListener("submit", function (e) {
        const form = e.target;
        if (!e.defaultPrevented && form?.matches?.("form") && form.dataset.loader !== "false") {
            mostrarCargando("Enviando informacion...");
        }
    });

    document.addEventListener("click", function (e) {
        const link = e.target.closest("a[href]");
        if (!link || link.dataset.loader === "false") return;
        if (link.target && link.target !== "_self") return;
        if (link.hasAttribute("download")) return;

        const href = link.getAttribute("href") || "";
        if (!href || href === "#" || href.startsWith("#")) return;
        if (href.startsWith("javascript:") || href.startsWith("mailto:") || href.startsWith("tel:")) return;

        mostrarCargando("Cargando pagina...");
    });
});

window.addEventListener("pageshow", () => ocultarCargando(true));

/*======================================================
=             1.1 SELECTS BUSCABLES DEL SISTEMA        =
======================================================*/

// Convierte los <select class="select2"> en controles buscables sin cambiar
// el select original. El valor se mantiene en el select nativo para que los
// formularios, validaciones y eventos change existentes sigan funcionando.
function inicializarSelectsBuscables(root = document) {
    root.querySelectorAll("select.select2:not([data-searchable-ready='true'])").forEach(select => {
        configurarSelectBuscable(select);
    });
}

function configurarSelectBuscable(select) {
    select.dataset.searchableReady = "true";
    select.classList.add("select2-native-hidden");

    const control = document.createElement("div");
    control.className = "searchable-select-control";
    control.tabIndex = 0;
    control.setAttribute("role", "combobox");
    control.setAttribute("aria-expanded", "false");

    const textoSeleccionado = document.createElement("span");
    textoSeleccionado.className = "searchable-select-value";
    control.appendChild(textoSeleccionado);
    select.insertAdjacentElement("afterend", control);

    let ultimoValorSincronizado = null;
    let ultimoTextoSincronizado = null;
    let ultimoEstadoDisabled = null;
    let indiceOpcionActiva = -1;

    const panel = document.createElement("div");
    panel.className = "searchable-select-panel";
    panel.style.display = "none";

    const inputBusqueda = document.createElement("input");
    inputBusqueda.type = "search";
    inputBusqueda.className = "searchable-select-search";
    inputBusqueda.placeholder = "Buscar...";
    inputBusqueda.autocomplete = "off";

    const listaOpciones = document.createElement("ul");
    listaOpciones.className = "searchable-select-options";
    listaOpciones.setAttribute("role", "listbox");

    panel.appendChild(inputBusqueda);
    panel.appendChild(listaOpciones);
    document.body.appendChild(panel);

    function obtenerPlaceholder() {
        const opcionVacia = Array.from(select.options).find(option => option.value === "");
        return opcionVacia?.textContent?.trim() || "Seleccione una opcion";
    }

    function sincronizarControl() {
        const opcionSeleccionada = select.options[select.selectedIndex];
        const texto = opcionSeleccionada?.textContent?.trim();
        const textoVisible = select.value ? texto : obtenerPlaceholder();

        if (
            ultimoValorSincronizado === select.value &&
            ultimoTextoSincronizado === textoVisible &&
            ultimoEstadoDisabled === select.disabled
        ) {
            return;
        }

        ultimoValorSincronizado = select.value;
        ultimoTextoSincronizado = textoVisible;
        ultimoEstadoDisabled = select.disabled;
        textoSeleccionado.textContent = textoVisible;
        textoSeleccionado.classList.toggle("searchable-select-placeholder", !select.value);
        control.classList.toggle("is-disabled", select.disabled);
        control.setAttribute("aria-disabled", String(select.disabled));
    }

    function posicionarPanel() {
        const rect = control.getBoundingClientRect();
        panel.style.left = `${rect.left}px`;
        panel.style.top = `${rect.bottom + 4}px`;
        panel.style.width = `${rect.width}px`;
    }

    function obtenerItemsOpciones() {
        return Array.from(listaOpciones.querySelectorAll(".searchable-select-option"));
    }

    function marcarOpcionActiva(indice) {
        const items = obtenerItemsOpciones();

        items.forEach(item => item.classList.remove("is-active"));

        if (!items.length) {
            indiceOpcionActiva = -1;
            inputBusqueda.removeAttribute("aria-activedescendant");
            return;
        }

        indiceOpcionActiva = Math.max(0, Math.min(indice, items.length - 1));
        const itemActivo = items[indiceOpcionActiva];
        itemActivo.classList.add("is-active");
        inputBusqueda.setAttribute("aria-activedescendant", itemActivo.id);
        itemActivo.scrollIntoView({ block: "nearest" });
    }

    function moverOpcionActiva(direccion) {
        const items = obtenerItemsOpciones();
        if (!items.length) return;

        const siguienteIndice = indiceOpcionActiva < 0
            ? (direccion > 0 ? 0 : items.length - 1)
            : (indiceOpcionActiva + direccion + items.length) % items.length;

        marcarOpcionActiva(siguienteIndice);
    }

    function seleccionarOpcion(option) {
        select.value = option.value;
        select.dispatchEvent(new Event("change", { bubbles: true }));
        cerrarPanel();
        control.focus();
    }

    function renderizarOpciones(filtro = "") {
        const textoFiltro = filtro.trim().toLowerCase();
        listaOpciones.innerHTML = "";
        indiceOpcionActiva = -1;

        const opciones = Array.from(select.options).filter(option => {
            if (option.disabled) return false;
            if (!textoFiltro) return true;
            return option.textContent.toLowerCase().includes(textoFiltro);
        });

        if (!opciones.length) {
            const vacio = document.createElement("li");
            vacio.className = "searchable-select-empty";
            vacio.textContent = "Sin resultados";
            listaOpciones.appendChild(vacio);
            return;
        }

        opciones.forEach((option, index) => {
            const item = document.createElement("li");
            item.className = "searchable-select-option";
            item.setAttribute("role", "option");
            item.textContent = option.textContent;
            item.dataset.value = option.value;
            item.id = `${select.id || select.name || "select"}-option-${index}`;
            item.classList.toggle("is-selected", option.value === select.value);
            item.setAttribute("aria-selected", String(option.value === select.value));

            item.addEventListener("click", () => {
                seleccionarOpcion(option);
            });

            listaOpciones.appendChild(item);
        });

        const indiceSeleccionado = opciones.findIndex(option => option.value === select.value);
        marcarOpcionActiva(indiceSeleccionado >= 0 ? indiceSeleccionado : 0);
    }

    function abrirPanel() {
        if (select.disabled) return;

        document.querySelectorAll(".searchable-select-panel").forEach(otroPanel => {
            if (otroPanel !== panel) otroPanel.style.display = "none";
        });
        document.querySelectorAll(".searchable-select-control.is-open").forEach(otroControl => {
            if (otroControl !== control) {
                otroControl.classList.remove("is-open");
                otroControl.setAttribute("aria-expanded", "false");
            }
        });

        posicionarPanel();
        inputBusqueda.value = "";
        renderizarOpciones();
        panel.style.display = "block";
        control.classList.add("is-open");
        control.setAttribute("aria-expanded", "true");
        inputBusqueda.focus();
    }

    function cerrarPanel() {
        panel.style.display = "none";
        control.classList.remove("is-open");
        control.setAttribute("aria-expanded", "false");
    }

    control.addEventListener("click", abrirPanel);
    control.addEventListener("keydown", event => {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            abrirPanel();
            return;
        }

        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            abrirPanel();
        }
    });

    inputBusqueda.addEventListener("input", () => renderizarOpciones(inputBusqueda.value));
    inputBusqueda.addEventListener("keydown", event => {
        if (event.key === "ArrowDown") {
            event.preventDefault();
            moverOpcionActiva(1);
            return;
        }

        if (event.key === "ArrowUp") {
            event.preventDefault();
            moverOpcionActiva(-1);
            return;
        }

        if (event.key === "Home") {
            event.preventDefault();
            marcarOpcionActiva(0);
            return;
        }

        if (event.key === "End") {
            event.preventDefault();
            marcarOpcionActiva(obtenerItemsOpciones().length - 1);
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            const itemActivo = obtenerItemsOpciones()[indiceOpcionActiva];
            const option = itemActivo
                ? Array.from(select.options).find(option => option.value === itemActivo.dataset.value)
                : null;

            if (option) {
                seleccionarOpcion(option);
            }

            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            cerrarPanel();
            control.focus();
        }
    });
    select.addEventListener("change", sincronizarControl);
    window.addEventListener("resize", () => {
        if (panel.style.display === "block") posicionarPanel();
    });
    window.addEventListener("scroll", () => {
        if (panel.style.display === "block") posicionarPanel();
    }, true);

    document.addEventListener("click", event => {
        if (!control.contains(event.target) && !panel.contains(event.target)) {
            cerrarPanel();
        }
    });

    new MutationObserver(sincronizarControl).observe(select, {
        childList: true,
        subtree: true,
        attributes: true,
        attributeFilter: ["disabled"]
    });

    setInterval(sincronizarControl, 300);
    sincronizarControl();
}

window.inicializarSelectsBuscables = inicializarSelectsBuscables;

function ajustarAnchoSidebar() {
    const sidebar = document.getElementById("sidebar");
    if (!sidebar || esVistaMovil() || document.body.classList.contains("sidebar-collapsed")) {
        return;
    }

    const linksVisibles = Array.from(sidebar.querySelectorAll(".nav-link"))
        .filter(link => link.offsetParent !== null);

    const anchoNecesario = linksVisibles.reduce((max, link) => {
        const texto = link.querySelector(".menu-text");
        const tieneFlecha = Boolean(link.querySelector(".submenu-arrow"));
        const niveles = contarNivelesSubmenu(link);
        const anchoTexto = texto?.scrollWidth || 0;

        // Icono + separacion + padding + sangria por nivel + flecha si aplica + aire derecho.
        const anchoLink = anchoTexto + 24 + 10 + 72 + (niveles * 38) + (tieneFlecha ? 42 : 0);
        return Math.max(max, anchoLink);
    }, 0);

    const anchoFinal = Math.max(170, Math.min(Math.ceil(anchoNecesario), 430));

    document.body.style.setProperty("--sidebar-expanded-width", `${anchoFinal}px`);
}

function contarNivelesSubmenu(elemento) {
    let niveles = 0;
    let actual = elemento.parentElement;

    while (actual) {
        if (actual.classList?.contains("submenu-items")) {
            niveles++;
        }

        actual = actual.parentElement;
    }

    return niveles;
}

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".submenu-toggle").forEach(btn => {
        btn.addEventListener("click", function (e) {
            e.preventDefault();

            // Abre/cierra el submenu padre sin navegar a otra pagina.
            const parent = this.closest(".submenu");
            const quedaraAbierto = !parent.classList.contains("open");
            const listaHermanos = parent.parentElement;

            listaHermanos?.querySelectorAll(":scope > .submenu.open").forEach(submenu => {
                if (submenu !== parent) {
                    cerrarSubmenuCompleto(submenu);
                }
            });

            if (quedaraAbierto) {
                parent.classList.add("open");
            } else {
                cerrarSubmenuCompleto(parent);
            }

            requestAnimationFrame(ajustarAnchoSidebar);
        });
    });

    ajustarAnchoSidebar();
    setTimeout(ajustarAnchoSidebar, 150);
    window.addEventListener("resize", ajustarAnchoSidebar);
});

function cerrarSubmenuCompleto(submenu) {
    submenu.classList.remove("open");
    submenu.querySelectorAll(".submenu.open").forEach(hijo => hijo.classList.remove("open"));
}

function esVistaMovil() {
    return window.matchMedia("(max-width: 991.98px)").matches;
}

function cerrarSidebarMovil() {
    document.body.classList.remove("sidebar-mobile-open");
    document.getElementById("sidebarToggle")?.setAttribute("aria-expanded", "false");
}

/*======================================================
=                 2. SIDEBAR (COLAPSAR)                =
======================================================*/

document.addEventListener("DOMContentLoaded", function () {

    const toggle = document.getElementById("sidebarToggle");
    if (!toggle) return;
    const backdrop = document.getElementById("sidebarBackdrop");

    toggle.setAttribute("aria-expanded", "false");

    toggle.addEventListener("click", function () {
        if (esVistaMovil()) {
            const quedoAbierto = document.body.classList.toggle("sidebar-mobile-open");
            toggle.setAttribute("aria-expanded", quedoAbierto ? "true" : "false");
            return;
        }

        document.body.classList.toggle("sidebar-collapsed");
        requestAnimationFrame(ajustarAnchoSidebar);
    });

    backdrop?.addEventListener("click", cerrarSidebarMovil);

    document.querySelectorAll("#sidebar .nav-link:not(.submenu-toggle)").forEach(link => {
        link.addEventListener("click", cerrarSidebarMovil);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            cerrarSidebarMovil();
        }
    });

    window.addEventListener("resize", () => {
        if (!esVistaMovil()) {
            cerrarSidebarMovil();
            requestAnimationFrame(ajustarAnchoSidebar);
        }
    });
});

/*======================================================
=         3. ACCIONES DEL CARD (COLAPSAR/EXPANDIR)     =
======================================================*/

document.addEventListener("DOMContentLoaded", function () {

    /* COLAPSAR */
    document.querySelectorAll("[data-action='collapse']").forEach(btn => {
        btn.addEventListener("click", function () {
            const cardBody = this.closest(".card").querySelector(".card-body");
            cardBody.classList.toggle("show");
        });
    });

    /* EXPANDIR (pantalla completa) */
    document.querySelectorAll("[data-action='expand']").forEach(btn => {
        btn.addEventListener("click", function () {
            const card = this.closest(".card");
            card.classList.toggle("card-fullscreen");
        });
    });

    /* CERRAR */
    document.querySelectorAll("[data-action='close']").forEach(btn => {
        btn.addEventListener("click", function () {
            const card = this.closest(".card");
            card.remove();
        });
    });

});

/*======================================================
=           4. MOSTRAR / OCULTAR CONTRASEÑA            =
======================================================*/

document.querySelectorAll(".toggle-password").forEach(icon => {
    icon.addEventListener("click", () => {

        const input = document.getElementById(icon.dataset.target);

        if (input.type === "password") {
            input.type = "text";
            icon.classList.replace("fa-eye", "fa-eye-slash");
        } else {
            input.type = "password";
            icon.classList.replace("fa-eye-slash", "fa-eye");
        }

    });
});

/*======================================================
=               5. VALIDACIÓN DE FORMULARIOS           =
======================================================*/

function mostrarError(inputId, mensaje) {

    const input = document.getElementById(inputId);

    // Limpia errores previos del mismo campo antes de pintar uno nuevo.
    input.classList.remove("form-error");
    const errorPrevio = input.parentElement.querySelector(".form-error-message");
    if (errorPrevio) errorPrevio.remove();

    input.classList.add("form-error");

    const error = document.createElement("div");
    error.classList.add("form-error-message");
    error.innerText = mensaje;

    // El mensaje queda junto al input para que el usuario sepa que corregir.
    input.parentElement.appendChild(error);
}

// Limpia todos los errores visuales del formulario activo.
function limpiarErrores() {
    document.querySelectorAll(".form-error").forEach(i => i.classList.remove("form-error"));
    document.querySelectorAll(".form-error-message").forEach(e => e.remove());
}

document.querySelectorAll("input").forEach(input => {
    input.addEventListener("input", () => {
        // Al escribir, se quita el error del campo para dar feedback inmediato.
        input.classList.remove("form-error");
        const error = input.parentElement.querySelector(".form-error-message");
        if (error) error.remove();
    });
});

/*======================================================
=     6. MENÚ DE OPCIONES (HAMBURGUESA EN TABLAS)      =
======================================================*/

document.addEventListener("click", function (e) {

    // 1. Ignorar clics dentro del sidebar
    if (e.target.closest("#sidebar")) return;

    // 2. Ignorar clics dentro del CONTENIDO del modal (pero NO los botones)
    if (e.target.closest("#modalEditar .modal-content-custom")) return;

    // 3. Botón hamburguesa
    const btn = e.target.closest(".action-menu");
    if (btn) {

        const id = btn.dataset.id;
        const menu = document.getElementById(`menu-${id}`);

        document.querySelectorAll(".dropdown-menu-custom").forEach(m => {
            if (m !== menu) m.style.display = "none";
        });

        menu.style.display = menu.style.display === "block" ? "none" : "block";
        return;
    }

    // 4. Cerrar menús si clic fuera
    if (!e.target.closest(".dropdown-menu-custom")) {
        document.querySelectorAll(".dropdown-menu-custom").forEach(m => m.style.display = "none");
    }

});

/*======================================================
=                 7. ALERTA SIMPLE (MODAL)             =
======================================================*/

function mostrarAlerta(tipo, titulo, mensaje) {

    // Modal global definido en _Layout.cshtml.
    const modal = document.getElementById("alertaSistema");
    const icono = document.getElementById("alertaIcono");
    const tituloEl = document.getElementById("alertaTitulo");
    const mensajeEl = document.getElementById("alertaMensaje");

    const iconos = {
        exito: "fa-circle-check",
        error: "fa-circle-xmark",
        advertencia: "fa-triangle-exclamation",
        info: "fa-circle-info"
    };

    // Cambia icono y contenido segun el tipo de alerta.
    icono.className = `fa-solid ${iconos[tipo] || iconos.info} modal-icon`;
    tituloEl.textContent = titulo;
    mensajeEl.textContent = mensaje;

    modal.style.display = "flex";

    // Cierra el modal sin ejecutar acciones adicionales.
    document.getElementById("alertaCerrar").onclick = () => {
        modal.style.display = "none";
    };
}

/*======================================================
=               8. CONFIRMACIÓN (MODAL)                =
======================================================*/

function mostrarConfirmacion(titulo, mensaje, callback) {

    // Modal global definido en _Layout.cshtml para decisiones Si/No.
    const modal = document.getElementById("confirmacionSistema");
    const tituloEl = document.getElementById("confirmacionTitulo");
    const mensajeEl = document.getElementById("confirmacionMensaje");

    tituloEl.textContent = titulo;
    mensajeEl.textContent = mensaje;

    modal.style.display = "flex";

    // Devuelve true al callback cuando el usuario confirma.
    document.getElementById("btnConfirmar").onclick = () => {
        modal.style.display = "none";
        callback(true);
    };

    // Devuelve false al callback cuando cancela.
    document.getElementById("btnCancelar").onclick = () => {
        modal.style.display = "none";
        callback(false);
    };
}
