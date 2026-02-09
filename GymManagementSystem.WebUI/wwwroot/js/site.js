// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
  const THEME_KEY = "gms-theme";
  const DARK_CLASS = "dark";

  function applyTheme(theme) {
    const root = document.documentElement;
    if (theme === "dark") {
      root.classList.add(DARK_CLASS);
    } else {
      root.classList.remove(DARK_CLASS);
    }
  }

  function getSavedTheme() {
    return localStorage.getItem(THEME_KEY) || "light";
  }

  function saveTheme(theme) {
    localStorage.setItem(THEME_KEY, theme);
  }

  function updateToggleText(theme) {
    const toggles = document.querySelectorAll("[data-theme-toggle]");
    toggles.forEach((btn) => {
      btn.textContent = theme === "dark" ? "Light Mode" : "Dark Mode";
    });
  }

  window.loadTheme = function () {
    const theme = getSavedTheme();
    applyTheme(theme);
    updateToggleText(theme);
  };

  window.toggleTheme = function () {
    const current = getSavedTheme();
    const next = current === "dark" ? "light" : "dark";
    saveTheme(next);
    applyTheme(next);
    updateToggleText(next);
  };

  document.addEventListener("DOMContentLoaded", () => {
    window.loadTheme();
    document.querySelectorAll("[data-theme-toggle]").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        e.preventDefault();
        window.toggleTheme();
      });
    });
  });
})();
