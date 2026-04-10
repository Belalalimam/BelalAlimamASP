document.addEventListener('DOMContentLoaded', function () {

    var trigger = document.querySelector('.trigger');
    var overlay = document.querySelector('.overlay');
    var miniClose = document.querySelector('.mini-close');
    var menuList = document.querySelector('.mobile-menu .menu-list');

    /* فتح المنيو */
    if (trigger) {
        trigger.addEventListener('click', function (e) {
            e.preventDefault();
            document.body.classList.add('showmenu');
        });
    }

    /* إغلاق المنيو - دالة مشتركة */
    function closeMenu() {
        document.body.classList.remove('showmenu');
        if (menuList) {
            menuList.classList.remove('show');
            menuList.querySelectorAll('.has-child.expand')
                .forEach(function (el) { el.classList.remove('expand'); });
        }
        document.querySelectorAll('.wider > div.active')
            .forEach(function (d) { d.classList.remove('active'); });
    }

    if (miniClose) miniClose.addEventListener('click', function (e) { e.preventDefault(); closeMenu(); });
    if (overlay) overlay.addEventListener('click', closeMenu);

    /* فتح الـ submenu */
    document.querySelectorAll('.mobile-menu .has-child > a').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var li = this.closest('.has-child');
            menuList.querySelectorAll('.has-child.expand').forEach(function (el) {
                if (el !== li) el.classList.remove('expand');
            });
            li.classList.add('expand');
            menuList.classList.add('show');
        });
    });

    /* زر Back */
    document.querySelectorAll('.mobile-menu .back > a').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var li = this.closest('.has-child');
            if (li) li.classList.remove('expand');
            menuList.classList.remove('show');
        });
    });

    /* Side Panels (bag, wishlist…) */
    document.querySelectorAll('.mobile-menu .mini [data-target]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            e.preventDefault();
            var targetId = this.getAttribute('data-target');
            var panel = document.querySelector('#' + targetId);
            document.querySelectorAll('.wider > div:not(.main-menu)').forEach(function (p) {
                if (p !== panel) p.classList.remove('active');
            });
            if (panel) panel.classList.toggle('active');
        });
    });

});