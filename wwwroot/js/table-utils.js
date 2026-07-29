// Componente reutilizable para construir tablas client-side desde una sola configuracion.
// La vista solo necesita un <table id="..."></table>; este archivo crea encabezado,
// cuerpo, celdas, acciones, busqueda, ordenamiento, paginacion e informacion.
window.Grilla = function Grilla(config) {
    const table = document.querySelector(config.tableSelector);
    if (!table) return null;

    const state = {
        originalData: [],
        filteredData: [],
        currentPage: 1,
        pageSize: config.defaultPageSize ?? 10,
        searchTerm: "",
        sortKey: config.defaultSortKey || null,
        sortDirection: config.defaultSortDirection || "asc",
        emptyText: config.emptyText || "No hay registros disponibles."
    };

    const features = {
        search: config.search !== false,
        sorting: config.sorting !== false,
        pagination: config.pagination !== false,
        pageSize: config.pageSize !== false,
        info: config.info !== false,
        responsive: config.responsive !== false
    };

    const pageSizeOptions = config.pageSizeOptions || [5, 10, 20, "all"];
    const rowNumberColumn = config.rowNumbers === false ? null : {
        type: "rowNumber",
        title: "Nro.",
        width: config.rowNumberWidth || 5,
        className: "text-center table-row-number",
        headerClassName: "text-center",
        sortable: false,
        searchable: false
    };
    const normalColumns = normalizeColumns(config.columns || []);
    const actionColumn = normalizeActions(config.actions);
    const columns = buildColumns(rowNumberColumn, normalColumns, actionColumn, config.actionsPosition);

    let tbody;
    let controls;

    initialize();

    // Prepara la tabla y crea los controles visuales antes y despues de ella.
    function initialize() {
        if (features.responsive && !table.parentElement.classList.contains("plantilla-table-responsive")) {
            table.parentElement.classList.add("plantilla-table-responsive");
        }

        table.classList.add("plantilla-table");
        table.innerHTML = "";
        buildColGroup();
        buildHeader();
        tbody = document.createElement("tbody");
        table.appendChild(tbody);
        controls = createControls();
        wireEvents();
    }

    // Normaliza columnas para soportar data, key o name como origen del valor.
    function normalizeColumns(rawColumns) {
        return rawColumns.map(column => ({
            ...column,
            key: column.key || column.data || column.name || null,
            sortable: column.sortable !== false,
            searchable: column.searchable !== false
        }));
    }

    // Convierte las acciones en una columna especial de opciones.
    function normalizeActions(actions) {
        if (!actions) return null;

        const actionConfig = Array.isArray(actions)
            ? { items: actions }
            : actions;

        return {
            type: "actions",
            title: actionConfig.title || "Opciones",
            width: actionConfig.width || 8,
            className: actionConfig.className || "text-center position-relative",
            headerClassName: actionConfig.headerClassName || "text-center",
            buttonClassName: actionConfig.buttonClassName || "btn btn-sm table-menu-btn",
            buttonIcon: actionConfig.buttonIcon || "fa-solid fa-list",
            items: actionConfig.items || []
        };
    }

    // Permite ubicar la columna de acciones al inicio o al final.
    function buildColumns(rowNumber, baseColumns, actions, actionsPosition) {
        const dataColumns = actions
            ? (actionsPosition === "start" ? [actions, ...baseColumns] : [...baseColumns, actions])
            : baseColumns;

        return rowNumber ? [rowNumber, ...dataColumns] : dataColumns;
    }

    // Aplica anchos por columna. Se aceptan numeros 1..100 o strings CSS como "120px".
    function buildColGroup() {
        const hasWidths = columns.some(column => column.width);
        if (!hasWidths) return;

        const colgroup = document.createElement("colgroup");
        columns.forEach(column => {
            const col = document.createElement("col");
            if (column.width) col.style.width = normalizeWidth(column.width);
            colgroup.appendChild(col);
        });
        table.appendChild(colgroup);
    }

    // Crea thead automaticamente con los titulos definidos en la configuracion.
    function buildHeader() {
        const thead = document.createElement("thead");
        thead.className = config.headerClassName || "table-header-blue";

        const tr = document.createElement("tr");
        columns.forEach(column => {
            const th = document.createElement("th");
            th.textContent = column.title || "";
            th.className = column.headerClassName || "";

            if (column.width) th.style.width = normalizeWidth(column.width);

            if (features.sorting && column.type !== "actions" && column.type !== "rowNumber" && column.sortable && column.key) {
                th.classList.add("table-sortable");
                th.dataset.sortKey = column.key;
            }

            tr.appendChild(th);
        });

        thead.appendChild(tr);
        table.appendChild(thead);
    }

    // Crea buscador, selector de cantidad, informacion y botones anterior/siguiente.
    function createControls() {
        const toolbar = document.createElement("div");
        toolbar.className = "table-tools";

        const left = document.createElement("div");
        left.className = "table-tools-left";

        const right = document.createElement("div");
        right.className = "table-tools-right";

        if (features.pageSize) {
            const label = document.createElement("label");
            label.className = "table-page-size";
            label.appendChild(document.createTextNode("Mostrar"));

            const select = document.createElement("select");
            select.className = "form-select form-select-sm table-page-size-select";

            pageSizeOptions.forEach(option => {
                const item = document.createElement("option");
                item.value = option;
                item.textContent = option === "all" ? "Todos" : `${option} registros`;
                select.appendChild(item);
            });

            select.value = state.pageSize;
            select.addEventListener("change", () => {
                state.pageSize = select.value === "all" ? "all" : Number(select.value);
                state.currentPage = 1;
                render();
            });

            label.appendChild(select);
            left.appendChild(label);
        }

        if (features.search) {
            const search = document.createElement("input");
            search.type = "search";
            search.className = "form-control form-control-sm table-search";
            search.placeholder = config.searchPlaceholder || "Buscar...";
            search.setAttribute("aria-label", "Buscar en la tabla");
            search.addEventListener("input", () => {
                state.searchTerm = search.value.trim().toLowerCase();
                state.currentPage = 1;
                render();
            });

            right.appendChild(search);
        }

        if (features.pageSize || features.search) {
            toolbar.appendChild(left);
            toolbar.appendChild(right);
            table.parentElement.insertBefore(toolbar, table);
        }

        const footer = document.createElement("div");
        footer.className = "table-footer-tools";

        const info = document.createElement("div");
        info.className = "table-info";

        const pagination = document.createElement("div");
        pagination.className = "table-pagination";

        if (features.info || features.pagination) {
            footer.appendChild(info);
            footer.appendChild(pagination);
            table.parentElement.insertBefore(footer, table.nextSibling);
        }

        return { info, pagination };
    }

    // Centraliza eventos para encabezados ordenables, menus y acciones.
    function wireEvents() {
        table.addEventListener("click", event => {
            const sortableHeader = event.target.closest(".table-sortable");
            if (sortableHeader) {
                changeSort(sortableHeader.dataset.sortKey);
                return;
            }

            const menuButton = event.target.closest(".table-menu-btn");
            if (menuButton) {
                toggleMenu(menuButton);
                return;
            }

            const actionButton = event.target.closest("[data-table-action]");
            if (actionButton) {
                runAction(actionButton);
            }
        });

        document.addEventListener("click", event => {
            if (!table.contains(event.target)) closeMenus();
        });
    }

    // Cambia el criterio de ordenamiento al hacer click en un encabezado.
    function changeSort(key) {
        if (!key) return;

        if (state.sortKey === key) {
            state.sortDirection = state.sortDirection === "asc" ? "desc" : "asc";
        } else {
            state.sortKey = key;
            state.sortDirection = "asc";
        }

        render();
    }

    // Muestra u oculta el menu de opciones de una fila.
    function toggleMenu(button) {
        const menu = button.parentElement.querySelector(".table-menu");
        const isOpen = menu.style.display === "block";

        closeMenus(menu);

        if (isOpen) {
            menu.style.display = "none";
            return;
        }

        positionMenu(button, menu);
    }

    // Posiciona el menu como flotante para que no quede recortado por el card o el scroll responsive.
    function positionMenu(button, menu) {
        const buttonRect = button.getBoundingClientRect();

        menu.style.display = "block";
        menu.style.visibility = "hidden";

        const menuWidth = menu.offsetWidth;
        const menuHeight = menu.offsetHeight;
        const margin = 8;
        let top = buttonRect.bottom + margin;
        let left = buttonRect.right - menuWidth;

        if (top + menuHeight > window.innerHeight - margin) {
            top = buttonRect.top - menuHeight - margin;
        }

        if (left + menuWidth > window.innerWidth - margin) {
            left = window.innerWidth - menuWidth - margin;
        }

        if (left < margin) left = margin;
        if (top < margin) top = margin;

        menu.style.top = `${top}px`;
        menu.style.left = `${left}px`;
        menu.style.right = "auto";
        menu.style.visibility = "visible";
    }

    // Ejecuta la accion configurada para la fila actual.
    function runAction(button) {
        const rowIndex = Number(button.dataset.rowIndex);
        const actionName = button.dataset.tableAction;
        const row = state.filteredData[rowIndex];
        const action = actionColumn?.items.find(item => item.action === actionName);

        closeMenus();

        if (!row || !action || action.disabled?.(row)) return;
        action.onClick?.(row);
    }

    // Cierra todos los menus de opciones excepto el que se indique.
    function closeMenus(except = null) {
        table.querySelectorAll(".table-menu").forEach(menu => {
            if (menu !== except) {
                menu.style.display = "none";
                menu.style.visibility = "visible";
            }
        });
    }

    function getValue(row, key) {
        if (!key) return "";
        return key.split(".").reduce((value, part) => value?.[part], row);
    }

    function normalizeText(value) {
        if (value === null || value === undefined) return "";
        return String(value).toLowerCase();
    }

    function normalizeWidth(width) {
        if (typeof width === "number") {
            return `${Math.min(100, Math.max(1, width))}%`;
        }

        return width;
    }

    function applySearch(data) {
        if (!features.search || !state.searchTerm) return data;

        const searchableColumns = normalColumns.filter(column => column.searchable && column.key);
        return data.filter(row =>
            searchableColumns.some(column => normalizeText(getValue(row, column.key)).includes(state.searchTerm))
        );
    }

    function applySort(data) {
        if (!features.sorting || !state.sortKey) return data;

        const sortColumn = normalColumns.find(column => column.key === state.sortKey);
        const direction = state.sortDirection === "asc" ? 1 : -1;

        return [...data].sort((a, b) => {
            const aValue = sortColumn?.sortValue ? sortColumn.sortValue(a) : getValue(a, state.sortKey);
            const bValue = sortColumn?.sortValue ? sortColumn.sortValue(b) : getValue(b, state.sortKey);

            if (aValue === bValue) return 0;
            if (aValue === null || aValue === undefined) return -1 * direction;
            if (bValue === null || bValue === undefined) return 1 * direction;

            if (typeof aValue === "number" && typeof bValue === "number") {
                return (aValue - bValue) * direction;
            }

            return String(aValue).localeCompare(String(bValue), "es", {
                sensitivity: "base",
                numeric: true
            }) * direction;
        });
    }

    function getPageData(data) {
        if (!features.pagination || state.pageSize === "all") return data;

        const start = (state.currentPage - 1) * state.pageSize;
        return data.slice(start, start + state.pageSize);
    }

    // Construye cada fila recorriendo la configuracion de columnas.
    function renderRow(row, rowIndex) {
        const tr = document.createElement("tr");

        columns.forEach(column => {
            const td = document.createElement("td");
            if (column.className) td.className = column.className;
            if (column.width) td.style.width = normalizeWidth(column.width);

            if (column.type === "rowNumber") {
                td.textContent = String(rowIndex + 1);
            } else if (column.type === "actions") {
                td.appendChild(renderActions(row, rowIndex, column));
            } else {
                const value = getValue(row, column.key);
                renderCellContent(td, column, value, row, rowIndex);
                column.createdCell?.(td, value, row, rowIndex);
            }

            tr.appendChild(td);
        });

        config.createdRow?.(tr, row, rowIndex);
        return tr;
    }

    // Pinta el contenido de una celda. Por defecto usa textContent para evitar HTML no confiable.
    function renderCellContent(td, column, value, row, rowIndex) {
        const rendered = column.render
            ? column.render(value, row, rowIndex)
            : value;

        if (rendered instanceof Node) {
            td.appendChild(rendered);
            return;
        }

        td.textContent = rendered ?? "";
    }

    // Crea el boton de opciones y los items visibles para la fila.
    function renderActions(row, rowIndex, column) {
        const wrapper = document.createElement("div");
        wrapper.className = "dropdown table-actions";

        const button = document.createElement("button");
        button.type = "button";
        button.className = column.buttonClassName;
        button.setAttribute("aria-label", "Opciones de la fila");

        const icon = document.createElement("i");
        icon.className = column.buttonIcon;
        button.appendChild(icon);

        const menu = document.createElement("div");
        menu.className = "table-menu";

        const visibleItems = column.items.filter(item => item.visible ? item.visible(row) : true);
        visibleItems.forEach(item => {
            const menuItem = document.createElement("button");
            menuItem.type = "button";
            menuItem.className = "table-menu-item";
            menuItem.dataset.tableAction = item.action;
            menuItem.dataset.rowIndex = rowIndex;
            menuItem.disabled = Boolean(item.disabled?.(row));

            if (item.icon) {
                const itemIcon = document.createElement("i");
                itemIcon.className = `${item.icon} me-2`;
                menuItem.appendChild(itemIcon);
            }

            menuItem.appendChild(document.createTextNode(item.label || item.action));
            menu.appendChild(menuItem);
        });

        if (visibleItems.length === 0) {
            button.disabled = true;
        }

        wrapper.appendChild(button);
        wrapper.appendChild(menu);
        return wrapper;
    }

    function updateHeaders() {
        table.querySelectorAll("thead th").forEach(header => {
            header.classList.remove("sort-asc", "sort-desc");
            if (header.dataset.sortKey === state.sortKey) {
                header.classList.add(state.sortDirection === "asc" ? "sort-asc" : "sort-desc");
            }
        });
    }

    function renderInfo(total, filtered) {
        if (!features.info || !controls.info) {
            controls.info.textContent = "";
            return;
        }

        if (filtered === 0) {
            controls.info.textContent = "Sin registros para mostrar";
            return;
        }

        const start = state.pageSize === "all" ? 1 : ((state.currentPage - 1) * state.pageSize) + 1;
        const end = state.pageSize === "all" ? filtered : Math.min(state.currentPage * state.pageSize, filtered);
        controls.info.textContent = `Mostrando ${start} a ${end} de ${filtered} registros${filtered !== total ? ` (filtrados de ${total})` : ""}`;
    }

    function renderPagination(filtered) {
        controls.pagination.textContent = "";

        if (!features.pagination || state.pageSize === "all") return;

        const totalPages = Math.max(1, Math.ceil(filtered / state.pageSize));
        state.currentPage = Math.min(state.currentPage, totalPages);

        const previous = createPaginationButton("Anterior", state.currentPage <= 1, () => {
            state.currentPage = Math.max(1, state.currentPage - 1);
            render();
        });

        const pageLabel = document.createElement("span");
        pageLabel.className = "table-page-label";
        pageLabel.textContent = `${state.currentPage} / ${totalPages}`;

        const next = createPaginationButton("Siguiente", state.currentPage >= totalPages, () => {
            state.currentPage = Math.min(totalPages, state.currentPage + 1);
            render();
        });

        controls.pagination.appendChild(previous);
        controls.pagination.appendChild(pageLabel);
        controls.pagination.appendChild(next);
    }

    function createPaginationButton(text, disabled, onClick) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "btn btn-sm btn-outline-light";
        button.textContent = text;
        button.disabled = disabled;
        button.addEventListener("click", onClick);
        return button;
    }

    function renderEmpty() {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = columns.length || 1;
        td.className = "text-center text-secondary py-3";
        td.textContent = state.emptyText;
        tr.appendChild(td);
        tbody.appendChild(tr);
    }

    // Aplica busqueda, ordenamiento, paginacion y vuelve a pintar el cuerpo.
    function render() {
        state.filteredData = applySort(applySearch(state.originalData));
        const pageData = getPageData(state.filteredData);
        const pageStartIndex = state.pageSize === "all" ? 0 : (state.currentPage - 1) * state.pageSize;

        tbody.textContent = "";

        if (pageData.length === 0) {
            renderEmpty();
        } else {
            pageData.forEach((row, index) => {
                tbody.appendChild(renderRow(row, pageStartIndex + index));
            });
        }

        updateHeaders();
        renderInfo(state.originalData.length, state.filteredData.length);
        renderPagination(state.filteredData.length);
    }

    return {
        setData(data) {
            state.originalData = Array.isArray(data) ? data : [];
            state.currentPage = 1;
            render();
        },
        refresh() {
            render();
        },
        clear() {
            state.originalData = [];
            state.currentPage = 1;
            render();
        },
        setEmptyText(text) {
            state.emptyText = text || config.emptyText || "No hay registros disponibles.";
            render();
        },
        getData() {
            return [...state.originalData];
        }
    };
};

window.PlantillaTable = window.Grilla;
