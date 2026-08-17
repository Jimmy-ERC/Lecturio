(function () {
    const searchInput = document.getElementById("librarySearch");
    const filterButtons = document.querySelectorAll("[data-estado-filter]");
    const cards = document.querySelectorAll(".book-card[data-estado]");

    let activeEstado = "Todos";

    function applyFilters() {
        const query = (searchInput?.value ?? "").trim().toLowerCase();

        cards.forEach((card) => {
            const matchesEstado = activeEstado === "Todos" || card.dataset.estado === activeEstado;
            const haystack = `${card.dataset.titulo ?? ""} ${card.dataset.autor ?? ""}`.toLowerCase();
            const matchesQuery = query === "" || haystack.includes(query);
            card.style.display = matchesEstado && matchesQuery ? "" : "none";
        });
    }

    searchInput?.addEventListener("input", applyFilters);

    filterButtons.forEach((btn) => {
        btn.addEventListener("click", () => {
            activeEstado = btn.dataset.estadoFilter;
            filterButtons.forEach((b) => {
                const isActive = b === btn;
                b.classList.toggle("btn-brand", isActive);
                b.classList.toggle("btn-outline-secondary", !isActive);
            });
            applyFilters();
        });
    });

    const deleteModalEl = document.getElementById("confirmDeleteModal");
    const deleteModal = deleteModalEl ? new bootstrap.Modal(deleteModalEl) : null;
    const deleteModalTitulo = document.getElementById("confirmDeleteTitulo");
    const confirmDeleteBtn = document.getElementById("confirmDeleteBtn");
    let formPendienteEliminar = null;

    document.querySelectorAll(".delete-libro-form").forEach((form) => {
        form.addEventListener("submit", (e) => {
            if (form.dataset.confirmado === "true") return;
            e.preventDefault();
            formPendienteEliminar = form;
            if (deleteModalTitulo) deleteModalTitulo.textContent = form.dataset.titulo || "este libro";
            deleteModal?.show();
        });
    });

    confirmDeleteBtn?.addEventListener("click", () => {
        if (formPendienteEliminar) {
            formPendienteEliminar.dataset.confirmado = "true";
            formPendienteEliminar.submit();
        }
        deleteModal?.hide();
    });

    const shareModalEl = document.getElementById("shareModal");
    const shareModal = shareModalEl ? new bootstrap.Modal(shareModalEl) : null;
    const shareLibroIdInput = document.getElementById("shareLibroId");
    const shareLibroTituloEl = document.getElementById("shareLibroTitulo");
    const shareEmailInput = document.getElementById("shareEmail");

    document.querySelectorAll(".share-libro-btn").forEach((btn) => {
        btn.addEventListener("click", () => {
            if (shareLibroIdInput) shareLibroIdInput.value = btn.dataset.libroId || "";
            if (shareLibroTituloEl) shareLibroTituloEl.textContent = btn.dataset.titulo || "";
            if (shareEmailInput) shareEmailInput.value = "";
            shareModal?.show();
        });
    });
})();
