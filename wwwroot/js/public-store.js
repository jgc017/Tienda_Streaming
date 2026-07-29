(function () {
    const cart = new Map();
    let storageKey = "tiendaStreamingCart:publica:general";
    let cartContext = "publica";
    let codigoCompraSaldo = 0;
    const formatter = new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    });

    document.addEventListener("DOMContentLoaded", () => {
        configureStorageKey();
        loadCart();
        cargarSaldoVendedor();

        document.querySelectorAll(".public-add-cart").forEach(button => {
            button.addEventListener("click", () => addProduct(button));
        });

        document.getElementById("publicCartToggle")?.addEventListener("click", toggleCart);
        document.getElementById("publicCartClose")?.addEventListener("click", closeCart);
        document.getElementById("publicCartCheckout")?.addEventListener("click", confirmPurchase);
        document.getElementById("btnValidarCodigoCompra")?.addEventListener("click", validarCodigoCompra);
        document.getElementById("purchaseResultClose")?.addEventListener("click", cerrarResultadoCompra);
        document.getElementById("purchaseResultCloseFooter")?.addEventListener("click", cerrarResultadoCompra);
        document.getElementById("purchaseResultCopyAll")?.addEventListener("click", copiarResultadoCompleto);
        renderCart();
    });

    function configureStorageKey() {
        const shell = document.querySelector(".public-store-shell");
        cartContext = shell?.dataset.cartContext === "interna" ? "interna" : "publica";
        const idTipoUsuario = Number(shell?.dataset.tipoUsuario || 0);
        storageKey = `tiendaStreamingCart:${cartContext}:${idTipoUsuario || "general"}`;
    }

    function addProduct(button) {
        const optionRow = button.closest(".public-product-option");
        const inputQty = optionRow?.querySelector(".public-product-qty");
        const qty = Math.max(1, Number(inputQty?.value || 1));
        const name = button.dataset.name;
        const option = getDurationOption(button);
        const productType = button.dataset.productType === "Combo" ? "Combo" : "Pantalla";
        const platformId = Number(button.dataset.platformId || 0);
        const comboId = Number(button.dataset.comboId || 0);

        if (!name || !option || (productType === "Pantalla" && !platformId) || (productType === "Combo" && !comboId)) {
            return;
        }

        addCartItem({ productType, platformId, comboId, name, option, qty });
    }

    function getDurationOption(button) {
        const option = {
            duration: Number(button.dataset.duration || 0),
            stock: Number(button.dataset.stock || 0),
            price: Number(button.dataset.price || 0)
        };

        return option.duration > 0 && option.stock > 0 && option.price > 0 ? option : null;
    }

    function addCartItem({ productType, platformId, comboId, name, option, qty }) {
        const baseId = productType === "Combo" ? `combo-${comboId}` : `plataforma-${platformId}`;
        const id = `${baseId}-dias-${option.duration}`;
        const current = cart.get(id) || {
            id,
            productType,
            platformId,
            comboId,
            name,
            duration: option.duration,
            price: option.price,
            qty: 0,
            stock: option.stock
        };

        current.stock = option.stock;
        current.price = option.price;
        current.duration = option.duration;
        current.productType = productType;
        current.platformId = platformId;
        current.comboId = comboId;
        current.qty = Math.min(option.stock, current.qty + Math.max(1, qty));
        cart.set(id, current);
        saveCart();
        renderCart();
        openCart();
    }

    function renderCart() {
        const itemsContainer = document.getElementById("publicCartItems");
        const countContainer = document.getElementById("publicCartCount");
        const totalContainer = document.getElementById("publicCartTotal");
        if (!itemsContainer || !countContainer || !totalContainer) return;

        const items = Array.from(cart.values());
        const totalQty = items.reduce((sum, item) => sum + item.qty, 0);
        const total = getCartTotal();

        countContainer.textContent = totalQty.toString();
        totalContainer.textContent = formatter.format(total);
        window.dispatchEvent(new CustomEvent("store-cart-updated", { detail: { totalQty } }));
        itemsContainer.innerHTML = "";

        if (!items.length) {
            const empty = document.createElement("p");
            empty.className = "public-cart-empty";
            empty.textContent = "No hay productos agregados.";
            itemsContainer.appendChild(empty);
            updateCheckoutState(totalQty);
            return;
        }

        items.forEach(item => {
            const row = document.createElement("article");
            row.className = "public-cart-item";
            row.innerHTML = `
                <div>
                    <strong>${escapeHtml(item.name)}</strong>
                    <span>${item.duration} dias - ${formatter.format(item.price)} x ${item.qty}</span>
                </div>
                <div class="public-cart-qty">
                    <button type="button" aria-label="Disminuir ${escapeHtml(item.name)}">-</button>
                    <span>${item.qty}</span>
                    <button type="button" aria-label="Aumentar ${escapeHtml(item.name)}">+</button>
                </div>
                <strong>${formatter.format(item.price * item.qty)}</strong>
            `;

            const buttons = row.querySelectorAll("button");
            buttons[0].addEventListener("click", () => changeQty(item.id, -1));
            buttons[1].addEventListener("click", () => changeQty(item.id, 1));
            buttons[1].disabled = item.stock > 0 && item.qty >= item.stock;
            itemsContainer.appendChild(row);
        });

        updateCheckoutState(totalQty);
    }

    function changeQty(id, delta) {
        const item = cart.get(id);
        if (!item) return;

        item.qty = item.stock > 0 ? Math.min(item.stock, item.qty + delta) : item.qty + delta;
        if (item.qty <= 0) {
            cart.delete(id);
        } else {
            cart.set(id, item);
        }

        saveCart();
        renderCart();
    }

    function loadCart() {
        try {
            const saved = JSON.parse(localStorage.getItem(storageKey) || "[]");
            if (!Array.isArray(saved)) return;

            saved.forEach(item => {
                if (!item?.id || !item?.name || Number(item.price) <= 0 || Number(item.qty) <= 0) return;

                const productType = item.productType === "Combo" ? "Combo" : "Pantalla";
                const platformId = Number(item.platformId || String(item.id).match(/plataforma-(\d+)/)?.[1] || 0);
                const comboId = Number(item.comboId || String(item.id).match(/combo-(\d+)/)?.[1] || 0);
                const duration = Number(item.duration || String(item.id).match(/dias-(\d+)/)?.[1] || 0);
                if ((productType === "Pantalla" && platformId <= 0) || (productType === "Combo" && comboId <= 0) || duration <= 0) return;

                cart.set(item.id, {
                    id: item.id,
                    productType,
                    platformId,
                    comboId,
                    name: item.name,
                    duration,
                    price: Number(item.price),
                    qty: Number(item.qty),
                    stock: Number(item.stock || 0)
                });
            });
            saveCart();
        } catch {
            localStorage.removeItem(storageKey);
        }
    }

    function saveCart() {
        localStorage.setItem(storageKey, JSON.stringify(Array.from(cart.values())));
    }

    function toggleCart() {
        document.getElementById("publicCartPanel")?.classList.toggle("is-open");
    }

    function openCart() {
        document.getElementById("publicCartPanel")?.classList.add("is-open");
    }

    function closeCart() {
        document.getElementById("publicCartPanel")?.classList.remove("is-open");
    }

    function updateCheckoutState(totalQty) {
        const checkout = document.getElementById("publicCartCheckout");
        if (checkout) checkout.disabled = totalQty <= 0;
    }

    function getCartTotal() {
        return Array.from(cart.values()).reduce((sum, item) => sum + item.price * item.qty, 0);
    }

    function validarCodigoCompra() {
        const codigo = document.getElementById("txtCodigoCompra")?.value.trim();
        const correo = document.getElementById("txtCompraCorreoCliente")?.value.trim();
        if (!codigo) {
            mostrarAlerta("advertencia", "Codigo requerido", "Ingresa el codigo de compra.");
            return;
        }

        if (!correo) {
            mostrarAlerta("advertencia", "Correo requerido", "Ingresa el correo asociado al codigo.");
            return;
        }

        fetch("/api/TiendaPublicaApi/P_ValidarCodigoCompra", {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": document.getElementById("csrfToken")?.value || ""
            },
            body: JSON.stringify({ codigo, correo_Cliente: correo })
        })
            .then(response => response.json().catch(() => null))
            .then(data => {
                if (data?.ok) {
                    codigoCompraSaldo = Number(data.data.saldo_Disponible || 0);
                    document.getElementById("publicCodigoSaldo").textContent = `Saldo codigo: ${formatter.format(codigoCompraSaldo)}`;
                    const nombreInput = document.getElementById("txtCompraNombreCliente");
                    const correoInput = document.getElementById("txtCompraCorreoCliente");
                    if (nombreInput) nombreInput.value = data.data.nombre_Cliente || "";
                    if (correoInput) correoInput.value = data.data.correo_Cliente || correo;
                    mostrarAlerta("exito", "Codigo validado", `Saldo disponible: ${formatter.format(codigoCompraSaldo)}`);
                } else {
                    codigoCompraSaldo = 0;
                    document.getElementById("publicCodigoSaldo").textContent = "Saldo codigo: $0";
                    const nombreInput = document.getElementById("txtCompraNombreCliente");
                    if (nombreInput) nombreInput.value = "";
                    mostrarAlerta("error", "Codigo invalido", data?.mensaje || "No fue posible validar el codigo.");
                }
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo validar el codigo."));
    }

    function cargarSaldoVendedor() {
        if (cartContext !== "interna") return;

        fetch("/api/TiendaInternaApi/F_GetSaldoBilletera", { credentials: "same-origin" })
            .then(response => response.json().catch(() => null))
            .then(data => {
                if (data?.ok) {
                    const saldo = Number(data.data.saldo || 0);
                    const badge = document.getElementById("sellerWalletBalance");
                    if (badge) badge.textContent = `Saldo: ${formatter.format(saldo)}`;
                }
            })
            .catch(() => { });
    }

    function confirmPurchase() {
        const shell = document.querySelector(".public-store-shell");
        const idTipoUsuario = Number(shell?.dataset.tipoUsuario || 0);
        const items = Array.from(cart.values()).map(item => ({
            tipo_Producto: item.productType,
            id_Plataforma: item.productType === "Pantalla" ? Number(item.platformId || 0) : null,
            id_Combo: item.productType === "Combo" ? Number(item.comboId || 0) : null,
            tiempo_Pantalla: Number(item.duration || 0),
            cantidad: item.qty
        })).filter(item => (item.id_Plataforma || item.id_Combo) && item.tiempo_Pantalla > 0 && item.cantidad > 0);

        if (!idTipoUsuario || !items.length) return;

        if (cartContext === "publica") {
            confirmarCompraPublica(idTipoUsuario, items);
            return;
        }

        confirmarCompraInterna(idTipoUsuario, items);
    }

    function confirmarCompraPublica(idTipoUsuario, items) {
        const nombre = document.getElementById("txtCompraNombreCliente")?.value.trim();
        const correo = document.getElementById("txtCompraCorreoCliente")?.value.trim();
        const codigo = document.getElementById("txtCodigoCompra")?.value.trim();

        if (!nombre) {
            mostrarAlerta("advertencia", "Codigo sin validar", "Valida el codigo para cargar el nombre del cliente.");
            return;
        }

        if (!correo) {
            mostrarAlerta("advertencia", "Correo requerido", "Ingresa el correo asociado al codigo.");
            return;
        }

        if (!codigo) {
            mostrarAlerta("advertencia", "Codigo requerido", "Ingresa el codigo de compra.");
            return;
        }

        if (codigoCompraSaldo > 0 && getCartTotal() > codigoCompraSaldo) {
            mostrarAlerta("error", "Saldo insuficiente", `El total supera el saldo validado: ${formatter.format(codigoCompraSaldo)}.`);
            return;
        }

        enviarCompra("/api/TiendaPublicaApi/P_ConfirmarCompraPublica", {
            id_Tipo_Usuario: idTipoUsuario,
            nombre_Cliente: nombre,
            correo_Cliente: correo,
            codigo_Compra: codigo,
            fecha_Compra: new Date().toISOString(),
            items
        });
    }

    function confirmarCompraInterna(idTipoUsuario, items) {
        enviarCompra("/api/TiendaInternaApi/P_ConfirmarCompra", {
            id_Tipo_Usuario: idTipoUsuario,
            nombre_Cliente: "Vendedor",
            correo_Cliente: null,
            fecha_Compra: new Date().toISOString(),
            items
        });
    }

    function enviarCompra(url, payload) {
        fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": document.getElementById("csrfToken")?.value || ""
            },
            body: JSON.stringify(payload)
        })
            .then(response => {
                if (response.status === 401) {
                    window.location.href = "/Account/Login";
                    return null;
                }
                return response.json().catch(() => null);
            })
            .then(data => {
                if (!data) return;
                if (data.ok) {
                    cart.clear();
                    saveCart();
                    renderCart();
                    closeCart();
                    cargarSaldoVendedor();
                    mostrarResultadoCompra(data.data);
                    return;
                }

                mostrarAlerta("error", "Error", data.mensaje || "No fue posible confirmar la compra.");
            })
            .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo confirmar la compra."));
    }

    function mostrarResultadoCompra(data) {
        const body = document.getElementById("purchaseResultBody");
        const modal = document.getElementById("purchaseResultModal");
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

        if (cartContext === "interna") {
            const detail = document.createElement("article");
            detail.className = "purchase-result-item";
            detail.innerHTML = `
                <pre>${escapeHtml(formatearDetalleVendedor(data))}</pre>
            `;
            body.appendChild(detail);
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
*Total Pagado:* ${formatter.format(Number(total || 0))}`;
    }

    function formatearDetalleVendedor(data) {
        const detalle = (data.detalles || [])
            .map(d => `${d.producto || ""} | ${d.cantidad || 0} | ${formatter.format(Number(d.valor_Unitario || 0))} | ${formatter.format(Number(d.subtotal || 0))}`)
            .join("\n");

        return `*Saldo restante en billetera:* ${formatter.format(Number(data.saldo_Restante || 0))}
*Detalle de la compra:* ${detalle}`;
    }

    function copiarResultadoCompleto() {
        const bloques = Array.from(document.querySelectorAll("#purchaseResultBody pre")).map(p => p.textContent);
        copiarTexto(bloques.join("\n\n"));
    }

    function copiarTexto(texto) {
        navigator.clipboard?.writeText(texto)
            .then(() => mostrarAlerta("exito", "Copiado", "Informacion copiada al portapapeles."))
            .catch(() => mostrarAlerta("advertencia", "No se pudo copiar", texto));
    }

    function cerrarResultadoCompra() {
        const modal = document.getElementById("purchaseResultModal");
        if (modal) modal.style.display = "none";
        window.location.reload();
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


