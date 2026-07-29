// Obtiene el token antifalsificacion generado en VwSistemaConfig.cshtml.
const defaultsSistemaConfig = {
    logoUrl: "/img/IMAGENIA.png",
    faviconUrl: "/favicon.ico",
    loginBackgroundUrl: "/img/auth-background.svg",
    videoUrl: ""
};

function getCsrfToken() {
    return document.getElementById("csrfToken")?.value || "";
}

// Wrapper de fetch usado por el flujo de imagenes y videos.
function secureFetch(url, options = {}) {
    const method = (options.method || "GET").toUpperCase();
    const headers = new Headers(options.headers || {});

    if (method !== "GET" && method !== "HEAD" && method !== "OPTIONS") {
        headers.set("X-CSRF-TOKEN", getCsrfToken());
    }

    return fetch(url, {
        ...options,
        headers,
        credentials: "same-origin"
    });
}

// Normaliza respuestas del API y maneja autenticacion/autorizacion.
async function parseJsonResponse(response) {
    if (response.status === 401) {
        window.location.href = "/Account/Login";
        return null;
    }

    if (response.status === 403) {
        mostrarAlerta("advertencia", "Acceso denegado", "No tienes permisos para realizar esta accion.");
        return null;
    }

    const data = await response.json().catch(() => null);
    if (!response.ok && !data) {
        mostrarAlerta("error", "Error", "No se pudo procesar la solicitud.");
        return null;
    }

    return data;
}

document.addEventListener("DOMContentLoaded", () => {
    F_GetSistemaVisualConfig();

    document.getElementById("BtnGuardarSistemaConfig")?.addEventListener("click", P_UdpSistemaVisualConfig);
    document.getElementById("BtnRestaurarSistemaConfig")?.addEventListener("click", () => pintarFormulario(defaultsSistemaConfig));
    document.getElementById("btnSubirLogoSistema")?.addEventListener("click", () => P_UploadImagenSistema("logo", "fileLogoSistema", "txtLogoSistema", "previewLogoSistema"));
    document.getElementById("btnSubirFaviconSistema")?.addEventListener("click", () => P_UploadImagenSistema("favicon", "fileFaviconSistema", "txtFaviconSistema", "previewFaviconSistema"));
    document.getElementById("btnSubirFondoLoginSistema")?.addEventListener("click", () => P_UploadImagenSistema("loginbackground", "fileFondoLoginSistema", "txtFondoLoginSistema", "previewFondoLoginSistema"));
    document.getElementById("btnSubirVideoSistema")?.addEventListener("click", P_UploadVideoSistema);

    document.getElementById("txtLogoSistema")?.addEventListener("input", () => mostrarPreviewImagen("txtLogoSistema", "previewLogoSistema"));
    document.getElementById("txtFaviconSistema")?.addEventListener("input", () => mostrarPreviewImagen("txtFaviconSistema", "previewFaviconSistema"));
    document.getElementById("txtFondoLoginSistema")?.addEventListener("input", () => mostrarPreviewImagen("txtFondoLoginSistema", "previewFondoLoginSistema"));
    document.getElementById("txtVideoSistema")?.addEventListener("input", mostrarPreviewVideo);
});

// F_GetSistemaVisualConfig: consulta y pinta las imagenes y videos guardados.
function F_GetSistemaVisualConfig() {
    secureFetch("/api/SistemaConfigApi/F_GetSistemaVisualConfig")
        .then(parseJsonResponse)
        .then(data => {
            if (!data || !data.ok) return;
            pintarFormulario(data.data);
        })
        .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo cargar Imagenes y Videos."));
}

// P_UdpSistemaVisualConfig: guarda las rutas actuales de logo, favicon, fondo y video.
function P_UdpSistemaVisualConfig() {
    mostrarConfirmacion(
        "Guardar Imagenes y Videos?",
        "Los cambios se veran al recargar las paginas del sistema.",
        (confirmado) => {
            if (!confirmado) return;

            subirMediosPendientes()
                .then(mediosListos => {
                    if (!mediosListos) return null;

                    const payload = obtenerPayloadSistemaConfig();
                    if (!payload) return null;

                    return secureFetch("/api/SistemaConfigApi/P_UdpSistemaVisualConfig", {
                        method: "PUT",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(payload)
                    });
                })
                .then(response => response ? parseJsonResponse(response) : null)
                .then(data => {
                    if (!data) return;

                    if (data.ok) {
                        mostrarAlerta("exito", "Actualizado", data.mensaje);
                        pintarFormulario(data.data);
                        actualizarLogoLoader(data.data.logoUrl);
                    } else {
                        mostrarAlerta("error", "Error", data.mensaje);
                    }
                })
                .catch(() => mostrarAlerta("advertencia", "Error inesperado", "No se pudo guardar Imagenes y Videos."));
        }
    );
}

// P_UploadImagenSistema: sube la imagen seleccionada y asigna la ruta devuelta al input correspondiente.
function P_UploadImagenSistema(tipoImagen, inputArchivoId, inputRutaId, previewId) {
    const archivo = document.getElementById(inputArchivoId)?.files?.[0];
    if (!archivo) {
        mostrarAlerta("advertencia", "Imagen requerida", "Selecciona una imagen antes de subirla.");
        return;
    }

    subirImagenSiPendiente(tipoImagen, inputArchivoId, inputRutaId, previewId, true);
}

// P_UploadVideoSistema: sube un video local y reemplaza el anterior si tambien era local.
function P_UploadVideoSistema() {
    const archivo = document.getElementById("fileVideoSistema")?.files?.[0];
    if (!archivo) {
        mostrarAlerta("advertencia", "Video requerido", "Selecciona un video antes de subirlo.");
        return;
    }

    subirVideoSiPendiente(true);
}

// subirMediosPendientes: antes de guardar, carga cualquier archivo seleccionado.
function subirMediosPendientes() {
    return subirImagenSiPendiente("logo", "fileLogoSistema", "txtLogoSistema", "previewLogoSistema")
        .then(ok => ok ? subirImagenSiPendiente("favicon", "fileFaviconSistema", "txtFaviconSistema", "previewFaviconSistema") : false)
        .then(ok => ok ? subirImagenSiPendiente("loginbackground", "fileFondoLoginSistema", "txtFondoLoginSistema", "previewFondoLoginSistema") : false)
        .then(ok => ok ? subirVideoSiPendiente(false) : false);
}

function subirImagenSiPendiente(tipoImagen, inputArchivoId, inputRutaId, previewId, mostrarExito = false) {
    const inputArchivo = document.getElementById(inputArchivoId);
    const archivo = inputArchivo?.files?.[0];

    if (!archivo) {
        return Promise.resolve(true);
    }

    const formData = new FormData();
    formData.append("imagen", archivo);
    formData.append("tipoImagen", tipoImagen);

    return secureFetch("/api/SistemaConfigApi/P_UploadImagenSistema", {
        method: "POST",
        body: formData
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return;

            if (data.ok) {
                document.getElementById(inputRutaId).value = data.data;
                mostrarPreviewImagen(inputRutaId, previewId);
                inputArchivo.value = "";
                if (mostrarExito) {
                    mostrarAlerta("exito", "Imagen cargada", data.mensaje);
                }
                return true;
            } else {
                mostrarAlerta("error", "No fue posible subir", data.mensaje);
                return false;
            }
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo subir la imagen.");
            return false;
        });
}

function subirVideoSiPendiente(mostrarExito = false) {
    const inputArchivo = document.getElementById("fileVideoSistema");
    const archivo = inputArchivo?.files?.[0];

    if (!archivo) {
        return Promise.resolve(true);
    }

    const formData = new FormData();
    formData.append("video", archivo);
    formData.append("videoActual", valorCampo("txtVideoSistema"));

    return secureFetch("/api/SistemaConfigApi/P_UploadVideoSistema", {
        method: "POST",
        body: formData
    })
        .then(parseJsonResponse)
        .then(data => {
            if (!data) return false;

            if (data.ok) {
                document.getElementById("txtVideoSistema").value = data.data;
                mostrarPreviewVideo();
                inputArchivo.value = "";
                if (mostrarExito) {
                    mostrarAlerta("exito", "Video cargado", data.mensaje);
                }
                return true;
            }

            mostrarAlerta("error", "No fue posible subir", data.mensaje);
            return false;
        })
        .catch(() => {
            mostrarAlerta("advertencia", "Error inesperado", "No se pudo subir el video.");
            return false;
        });
}

function obtenerPayloadSistemaConfig() {
    const logoUrl = valorCampo("txtLogoSistema");
    const faviconUrl = valorCampo("txtFaviconSistema");
    const loginBackgroundUrl = valorCampo("txtFondoLoginSistema");
    const videoUrl = valorCampo("txtVideoSistema");

    if (!logoUrl) { mostrarError("txtLogoSistema", "El logo es obligatorio"); return null; }
    if (!faviconUrl) { mostrarError("txtFaviconSistema", "El favicon es obligatorio"); return null; }
    if (!loginBackgroundUrl) { mostrarError("txtFondoLoginSistema", "El fondo del login es obligatorio"); return null; }

    return { logoUrl, faviconUrl, loginBackgroundUrl, videoUrl };
}

function pintarFormulario(config) {
    document.getElementById("txtLogoSistema").value = config.logoUrl || defaultsSistemaConfig.logoUrl;
    document.getElementById("txtFaviconSistema").value = config.faviconUrl || defaultsSistemaConfig.faviconUrl;
    document.getElementById("txtFondoLoginSistema").value = config.loginBackgroundUrl || defaultsSistemaConfig.loginBackgroundUrl;
    document.getElementById("txtVideoSistema").value = config.videoUrl || "";

    mostrarPreviewImagen("txtLogoSistema", "previewLogoSistema");
    mostrarPreviewImagen("txtFaviconSistema", "previewFaviconSistema");
    mostrarPreviewImagen("txtFondoLoginSistema", "previewFondoLoginSistema");
    mostrarPreviewVideo();
}

function valorCampo(id) {
    return document.getElementById(id).value.trim();
}

function mostrarPreviewImagen(inputRutaId, previewId) {
    const ruta = valorCampo(inputRutaId);
    const preview = document.getElementById(previewId);

    if (!ruta) {
        preview.removeAttribute("src");
        preview.classList.add("d-none");
        return;
    }

    preview.src = ruta;
    preview.classList.remove("d-none");
}

function mostrarPreviewVideo() {
    const ruta = valorCampo("txtVideoSistema");
    const preview = document.getElementById("previewVideoSistema");
    const link = document.getElementById("linkPreviewVideoSistema");

    preview.pause();
    preview.removeAttribute("src");
    preview.classList.add("d-none");
    link.classList.add("d-none");
    link.removeAttribute("href");

    if (!ruta) {
        return;
    }

    if (esArchivoVideoLocal(ruta)) {
        preview.src = ruta;
        preview.classList.remove("d-none");
        return;
    }

    const youtubeWatchUrl = obtenerYoutubeWatchUrl(ruta);
    if (youtubeWatchUrl) {
        link.href = youtubeWatchUrl;
        link.innerHTML = '<i class="fa-brands fa-youtube me-1"></i> Abrir video';
        link.classList.remove("d-none");
        return;
    }

    link.href = ruta;
    link.innerHTML = '<i class="fa-solid fa-arrow-up-right-from-square me-1"></i> Abrir referencia';
    link.classList.remove("d-none");
}

function obtenerYoutubeEmbedUrl(url) {
    const videoId = obtenerYoutubeVideoId(url);
    return videoId
        ? `https://www.youtube.com/embed/${videoId}?autoplay=1&mute=1&playsinline=1&rel=0`
        : null;
}

function obtenerYoutubeWatchUrl(url) {
    const videoId = obtenerYoutubeVideoId(url);
    return videoId
        ? `https://www.youtube.com/watch?v=${videoId}`
        : null;
}

function obtenerYoutubeVideoId(url) {
    try {
        const uri = new URL(url);
        const host = uri.hostname.toLowerCase();
        let videoId = "";

        if (host.includes("youtu.be")) {
            videoId = limpiarYoutubeId(uri.pathname.replace(/^\/+/, ""));
        } else if (host.includes("youtube.com") || host.includes("youtube-nocookie.com")) {
            const segmentos = uri.pathname.split("/").filter(Boolean);

            if (["embed", "shorts", "live", "v"].includes(segmentos[0])) {
                videoId = limpiarYoutubeId(segmentos[1]);
            } else if (uri.pathname.toLowerCase() === "/watch") {
                videoId = limpiarYoutubeId(uri.searchParams.get("v"));
            }
        }

        return videoId || null;
    } catch {
        return null;
    }
}

function limpiarYoutubeId(value) {
    return (value || "").split(/[/?&#]/)[0].replace(/[^a-zA-Z0-9_-]/g, "");
}

function esArchivoVideoLocal(ruta) {
    const limpia = ruta.split("?")[0].split("#")[0].toLowerCase();
    return limpia.startsWith("/video/") && (limpia.endsWith(".mp4") || limpia.endsWith(".webm") || limpia.endsWith(".ogg"));
}

function actualizarLogoLoader(logoUrl) {
    const meta = document.querySelector("meta[name='app-logo']");
    if (meta) meta.setAttribute("content", logoUrl);

    const loaderLogo = document.querySelector(".global-loading-image");
    if (loaderLogo) loaderLogo.src = logoUrl;
}
