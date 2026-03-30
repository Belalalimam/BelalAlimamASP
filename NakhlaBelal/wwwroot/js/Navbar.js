console.log("dropdown js loaded");

document.addEventListener("DOMContentLoaded", function () {

    const dropdowns = document.querySelectorAll(".dropdown1");

    dropdowns.forEach(function (dropdown) {
        const btn = dropdown.querySelector(".dropdown1-btn");
        const menu = dropdown.querySelector(".dropdown1-menu");

        if (!btn || !menu) return;

        btn.addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();

            // close all
            document.querySelectorAll(".dropdown1-menu").forEach(m => {
                if (m !== menu) m.classList.remove("show");
            });

            menu.classList.toggle("show");
        });
    });

    document.addEventListener("click", function () {
        document.querySelectorAll(".dropdown1-menu").forEach(menu => {
            menu.classList.remove("show");
        });
    });

});
