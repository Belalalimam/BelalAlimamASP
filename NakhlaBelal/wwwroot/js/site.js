/* ==========================================
   LOGIN PAGE
   ========================================== */
var _c2 = document.getElementById('container2');
var _reg = document.getElementById('register');
var _log = document.getElementById('login');
if (_c2 && _reg && _log) {
    _reg.addEventListener('click', function () { _c2.classList.add('active'); });
    _log.addEventListener('click', function () { _c2.classList.remove('active'); });
}

/* ==========================================
   DROPDOWN + MOBILE MENU
   ========================================== */
document.addEventListener('DOMContentLoaded', function () {

    /* ====== Desktop Dropdown (نفس كودك القديم الشغّال) ====== */
    document.querySelectorAll('.dropdown1').forEach(function (dropdown) {
        var btn = dropdown.querySelector('.dropdown1-btn');
        var menu = dropdown.querySelector('.dropdown1-menu');
        if (!btn || !menu) return;

        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            document.querySelectorAll('.dropdown1-menu').forEach(function (m) {
                if (m !== menu) m.classList.remove('show');
            });

            menu.classList.toggle('show');
        });
    });

    document.addEventListener('click', function () {
        document.querySelectorAll('.dropdown1-menu').forEach(function (m) {
            m.classList.remove('show');
        });
    });

    /* ====== أيقونات الـ mini sidebar ====== */
    document.querySelectorAll('.mobile-menu .mini ul li a[data-target]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var targetId = this.getAttribute('data-target');
            var panel = document.getElementById(targetId);
            if (!panel) return;

            var isActive = panel.classList.contains('active');
            document.querySelectorAll('.wider > div').forEach(function (p) {
                p.classList.remove('active');
            });
            if (!isActive) panel.classList.add('active');
        });
    });

    /* ====== الـ submenu (has-child) ====== */
    document.querySelectorAll('.mobile-menu .menu-list .has-child > a').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var li = this.parentElement;
            var menuList = li.closest('.menu-list');
            menuList.classList.add('show');
            li.classList.add('expand');
        });
    });

    document.querySelectorAll('.mobile-menu .menu-list .back > a').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var parentHasChild = this.closest('.has-child');
            var menuList = this.closest('.menu-list');
            if (parentHasChild) parentHasChild.classList.remove('expand');
            if (menuList) menuList.classList.remove('show');
        });
    });

});