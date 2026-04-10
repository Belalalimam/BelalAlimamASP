/* ==========================================
   LOGIN PAGE (صفحة التسجيل فقط)
   ========================================== */
var container2 = document.getElementById('container2');
var registerBtn = document.getElementById('register');
var loginBtn = document.getElementById('login');

if (container2 && registerBtn && loginBtn) {
    registerBtn.addEventListener('click', function () {
        container2.classList.add('active');
    });
    loginBtn.addEventListener('click', function () {
        container2.classList.remove('active');
    });
}

/* ==========================================
   MOBILE MENU + DROPDOWNS
   ========================================== */
document.addEventListener('DOMContentLoaded', function () {

    /* فتح / إغلاق القائمة */
    var trigger = document.querySelector('.trigger');
    var overlay = document.querySelector('.overlay');
    var miniClose = document.querySelector('.mobile-menu .mini-close');

    function openMenu() {
        document.body.classList.add('showmenu');
    }
    function closeMenu() {
        document.body.classList.remove('showmenu');
        document.querySelectorAll('.wider > div').forEach(function (p) {
            p.classList.remove('active');
        });
    }

    if (trigger) trigger.addEventListener('click', openMenu);
    if (overlay) overlay.addEventListener('click', closeMenu);
    if (miniClose) miniClose.addEventListener('click', closeMenu);

    /* أيقونات الـ mini sidebar */
    document.querySelectorAll('.mobile-menu .mini ul li a').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            var targetId = this.getAttribute('data-target');
            var href = this.getAttribute('href');

            if (!targetId && href && href !== '#') return;

            e.preventDefault();
            e.stopPropagation();

            if (!targetId) return;

            var panel = document.getElementById(targetId);
            if (!panel) return;

            var isActive = panel.classList.contains('active');

            document.querySelectorAll('.wider > div').forEach(function (p) {
                p.classList.remove('active');
            });

            if (!isActive) panel.classList.add('active');
        });
    });

    /* الـ submenu (has-child) */
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

    /* Desktop dropdown */
    document.querySelectorAll('header .dropdown1').forEach(function (dropdown) {
        var btn = dropdown.querySelector('.dropdown1-btn');
        var menu = dropdown.querySelector('.dropdown1-menu');
        if (!btn || !menu) return;

        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var isOpen = menu.classList.contains('show');

            document.querySelectorAll('header .dropdown1-menu').forEach(function (m) { m.classList.remove('show'); });
            document.querySelectorAll('header .dropdown1').forEach(function (d) { d.classList.remove('open'); });

            if (!isOpen) {
                menu.classList.add('show');
                dropdown.classList.add('open');
            }
        });
    });

    document.addEventListener('click', function () {
        document.querySelectorAll('header .dropdown1-menu').forEach(function (m) { m.classList.remove('show'); });
        document.querySelectorAll('header .dropdown1').forEach(function (d) { d.classList.remove('open'); });
    });

});