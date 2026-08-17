(function () {
  "use strict";

  var CHIP_COLORS = [
    { bg: "#fdecea", fg: "#b3122a" },
    { bg: "#eaf4fb", fg: "#1f6fa8" },
    { bg: "#eafaf1", fg: "#1f7a4f" },
    { bg: "#fff6e0", fg: "#8a6300" },
    { bg: "#f3ecfb", fg: "#5a1f7a" },
    { bg: "#feeef2", fg: "#a3184f" },
    { bg: "#e9f7f7", fg: "#146a6a" },
    { bg: "#f1f0ea", fg: "#5a4a1f" },
  ];

  function colorFor(value) {
    var hash = 0;
    for (var i = 0; i < value.length; i++) {
      hash = (hash * 31 + value.charCodeAt(i)) >>> 0;
    }
    return CHIP_COLORS[hash % CHIP_COLORS.length];
  }

  function enhance(select) {
    if (select.dataset.enhanced === "true") return;
    select.dataset.enhanced = "true";

    var options = Array.prototype.map.call(select.options, function (o) {
      return { value: o.value, text: o.text };
    });

    var wrapper = document.createElement("div");
    wrapper.className = "genero-picker";

    var control = document.createElement("div");
    control.className = "genero-picker-control form-control";

    var chipsHost = document.createElement("div");
    chipsHost.className = "genero-picker-chips";

    var input = document.createElement("input");
    input.type = "text";
    input.className = "genero-picker-input";
    input.autocomplete = "off";
    input.placeholder = "Selecciona géneros…";
    input.setAttribute("aria-label", "Buscar géneros");

    control.appendChild(chipsHost);
    control.appendChild(input);

    var menu = document.createElement("ul");
    menu.className = "genero-picker-menu";
    menu.setAttribute("role", "listbox");
    menu.hidden = true;

    wrapper.appendChild(control);
    wrapper.appendChild(menu);

    select.classList.add("visually-hidden");
    select.setAttribute("aria-hidden", "true");
    select.tabIndex = -1;
    select.parentNode.insertBefore(wrapper, select.nextSibling);

    function findOption(value) {
      return Array.prototype.find.call(select.options, function (o) {
        return o.value === value;
      });
    }

    function isSelected(value) {
      var opt = findOption(value);
      return !!opt && opt.selected;
    }

    function setSelected(value, selected) {
      var opt = findOption(value);
      if (!opt) return;
      opt.selected = selected;
      select.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function renderChips() {
      chipsHost.innerHTML = "";
      options.forEach(function (opt) {
        if (!isSelected(opt.value)) return;
        var color = colorFor(opt.value);

        var chip = document.createElement("span");
        chip.className = "genero-chip";
        chip.style.backgroundColor = color.bg;
        chip.style.color = color.fg;
        chip.textContent = opt.text;

        var remove = document.createElement("button");
        remove.type = "button";
        remove.className = "genero-chip-remove";
        remove.setAttribute("aria-label", "Quitar " + opt.text);
        remove.textContent = "×";
        remove.addEventListener("click", function (e) {
          e.stopPropagation();
          setSelected(opt.value, false);
          renderAll();
          input.focus();
        });

        chip.appendChild(remove);
        chipsHost.appendChild(chip);
      });
    }

    function renderMenu() {
      var query = input.value.trim().toLowerCase();
      menu.innerHTML = "";
      var visibleCount = 0;

      options.forEach(function (opt) {
        if (query && opt.text.toLowerCase().indexOf(query) === -1) return;
        visibleCount++;

        var selected = isSelected(opt.value);

        var item = document.createElement("li");
        item.className = "genero-picker-option" + (selected ? " is-selected" : "");
        item.setAttribute("role", "option");
        item.setAttribute("aria-selected", selected ? "true" : "false");
        item.textContent = opt.text;

        item.addEventListener("mousedown", function (e) {
          e.preventDefault();
          setSelected(opt.value, !isSelected(opt.value));
          renderAll();
          input.value = "";
          input.focus();
          openMenu();
        });

        menu.appendChild(item);
      });

      if (visibleCount === 0) {
        var empty = document.createElement("li");
        empty.className = "genero-picker-empty";
        empty.textContent = "Sin resultados";
        menu.appendChild(empty);
      }
    }

    function renderAll() {
      renderChips();
      renderMenu();
    }

    function openMenu() {
      menu.hidden = false;
      wrapper.classList.add("is-open");
    }

    function closeMenu() {
      menu.hidden = true;
      wrapper.classList.remove("is-open");
    }

    control.addEventListener("click", function () {
      input.focus();
      openMenu();
    });

    input.addEventListener("focus", openMenu);

    input.addEventListener("input", function () {
      renderMenu();
      openMenu();
    });

    input.addEventListener("keydown", function (e) {
      if (e.key === "Backspace" && input.value === "") {
        var selected = options.filter(function (o) {
          return isSelected(o.value);
        });
        var last = selected[selected.length - 1];
        if (last) {
          setSelected(last.value, false);
          renderAll();
        }
      } else if (e.key === "Escape") {
        closeMenu();
        input.blur();
      }
    });

    document.addEventListener("click", function (e) {
      if (!wrapper.contains(e.target)) {
        closeMenu();
      }
    });

    renderAll();
  }

  document.addEventListener("DOMContentLoaded", function () {
    Array.prototype.forEach.call(
      document.querySelectorAll("select.js-genero-picker"),
      enhance
    );
  });
})();
