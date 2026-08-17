const reader = document.getElementById("reader");
const statusEl = document.getElementById("readerStatus");
const viewportEl = document.getElementById("readerViewport");
const canvas = document.getElementById("pdfCanvas");
const ctx = canvas.getContext("2d");

const pageNumEl = document.getElementById("pageNum");
const pageCountEl = document.getElementById("pageCount");
const prevBtns = [document.getElementById("prevPage"), document.getElementById("prevPageEdge")];
const nextBtns = [document.getElementById("nextPage"), document.getElementById("nextPageEdge")];
const zoomInBtn = document.getElementById("zoomIn");
const zoomOutBtn = document.getElementById("zoomOut");
const fullscreenBtn = document.getElementById("fullscreenToggle");

const pdfUrl = reader.dataset.pdfUrl;
const pdfJsSrc = reader.dataset.pdfJs;
const pdfWorkerSrc = reader.dataset.pdfWorker;
const libroId = reader.dataset.libroId;
const paginaInicial = parseInt(reader.dataset.paginaInicial, 10) || 1;
const progresoUrl = reader.dataset.progresoUrl;
const antiforgeryToken = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]')?.value ?? "";

const MIN_SCALE = 0.5;
const MAX_SCALE = 3;
const SCALE_STEP = 0.2;

let pdfDoc = null;
let pageNum = 1;
let scale = 1.25;
let renderTask = null;
let pendingPageNum = null;

function setStatus(text, isError = false) {
    statusEl.hidden = false;
    statusEl.textContent = text;
    statusEl.classList.toggle("reader-status-error", isError);
}

function clearStatus() {
    statusEl.hidden = true;
}

function updateNavButtons() {
    const atFirst = pageNum <= 1;
    const atLast = pdfDoc ? pageNum >= pdfDoc.numPages : true;
    for (const btn of prevBtns) btn.disabled = atFirst;
    for (const btn of nextBtns) btn.disabled = atLast;
}

async function renderPage(num) {
    if (!pdfDoc) return;

    if (renderTask) {
        pendingPageNum = num;
        return;
    }

    const page = await pdfDoc.getPage(num);
    const viewport = page.getViewport({ scale });
    canvas.width = viewport.width;
    canvas.height = viewport.height;
    canvas.hidden = false;

    renderTask = page.render({ canvasContext: ctx, viewport });
    try {
        await renderTask.promise;
    } finally {
        renderTask = null;
    }

    pageNum = num;
    pageNumEl.textContent = String(pageNum);
    updateNavButtons();
    viewportEl.scrollTop = 0;
    scheduleSaveProgress();

    if (pendingPageNum !== null) {
        const next = pendingPageNum;
        pendingPageNum = null;
        await renderPage(next);
    }
}

function goToPage(num) {
    if (!pdfDoc) return;
    const clamped = Math.min(Math.max(num, 1), pdfDoc.numPages);
    if (clamped === pageNum && canvas.hidden === false) return;
    renderPage(clamped);
}

function setScale(next) {
    scale = Math.min(Math.max(next, MIN_SCALE), MAX_SCALE);
    renderPage(pageNum);
}

async function loadPdf() {
    const pdfjsLib = await import(pdfJsSrc);
    pdfjsLib.GlobalWorkerOptions.workerSrc = pdfWorkerSrc;

    setStatus("Cargando documento…");
    pdfDoc = await pdfjsLib.getDocument({ url: pdfUrl }).promise;

    pageCountEl.textContent = String(pdfDoc.numPages);
    clearStatus();

    const startPage = Math.min(Math.max(paginaInicial, 1), pdfDoc.numPages);
    await renderPage(startPage);
}

loadPdf().catch((err) => {
    console.error("No se pudo cargar el PDF:", err);
    setStatus("No se pudo cargar el documento. Intenta recargar la página.", true);
});

for (const btn of prevBtns) btn.addEventListener("click", () => goToPage(pageNum - 1));
for (const btn of nextBtns) btn.addEventListener("click", () => goToPage(pageNum + 1));
zoomInBtn.addEventListener("click", () => setScale(scale + SCALE_STEP));
zoomOutBtn.addEventListener("click", () => setScale(scale - SCALE_STEP));

fullscreenBtn.addEventListener("click", () => {
    if (document.fullscreenElement) {
        document.exitFullscreen();
    } else {
        reader.requestFullscreen().catch(() => {});
    }
});

window.addEventListener("keydown", (e) => {
    if (e.key === "ArrowRight" || e.key === "PageDown") goToPage(pageNum + 1);
    else if (e.key === "ArrowLeft" || e.key === "PageUp") goToPage(pageNum - 1);
    else if (e.key === "+") setScale(scale + SCALE_STEP);
    else if (e.key === "-") setScale(scale - SCALE_STEP);
});

// --- Progreso de lectura: se guarda con un pequeño debounce al cambiar de página,
// y se fuerza a guardar de inmediato al salir de la pestaña o cerrarla, para poder
// retomar la lectura exactamente donde se quedó.
const SAVE_DEBOUNCE_MS = 800;
let saveTimer = null;
let lastSavedPage = null;

function buildProgresoBody() {
    const params = new URLSearchParams();
    params.set("id", libroId);
    params.set("pagina", String(pageNum));
    params.set("totalPaginas", String(pdfDoc ? pdfDoc.numPages : 0));
    params.set("__RequestVerificationToken", antiforgeryToken);
    return params;
}

function flushSaveProgress() {
    clearTimeout(saveTimer);
    if (!pdfDoc || !progresoUrl || pageNum === lastSavedPage) return;
    lastSavedPage = pageNum;
    fetch(progresoUrl, {
        method: "POST",
        body: buildProgresoBody(),
        credentials: "same-origin",
        keepalive: true,
    }).catch(() => {});
}

function scheduleSaveProgress() {
    clearTimeout(saveTimer);
    saveTimer = setTimeout(flushSaveProgress, SAVE_DEBOUNCE_MS);
}

document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") flushSaveProgress();
});
window.addEventListener("pagehide", flushSaveProgress);

// --- Auto-hide de controles: visibles solo al mover el mouse o tocar la pantalla,
// se desvanecen tras un período de inactividad (igual que el visor de Google Drive).
// El teclado (flechas para pasar de página, etc.) no debe reactivarlos.
const HIDE_DELAY_MS = 2500;
let hideTimer = null;
let toolbarHovered = false;

function scheduleHide() {
    clearTimeout(hideTimer);
    if (toolbarHovered) return;
    hideTimer = setTimeout(() => reader.classList.add("controls-hidden"), HIDE_DELAY_MS);
}

function showControls() {
    reader.classList.remove("controls-hidden");
    scheduleHide();
}

["mousemove", "mousedown", "touchstart"].forEach((evt) =>
    window.addEventListener(evt, showControls, { passive: true })
);

const toolbar = document.getElementById("readerToolbar");
toolbar.addEventListener("mouseenter", () => {
    toolbarHovered = true;
    clearTimeout(hideTimer);
    reader.classList.remove("controls-hidden");
});
toolbar.addEventListener("mouseleave", () => {
    toolbarHovered = false;
    scheduleHide();
});

showControls();
