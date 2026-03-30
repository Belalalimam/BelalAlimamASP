const menuTrigger = document.querySelector('.trigger');
const closeTrigger = document.querySelector('.mini-close');
const giveClass = document.querySelector('.site');

menuTrigger.addEventListener('click', function () {
    giveClass.classList.add('showmenu');
});

closeTrigger.addEventListener('click', function () {
    giveClass.classList.remove('showmenu');
});



//submenu
const button = document.querySelectorAll('.has-child > a'),
    modalheight = document.querySelector('.menu-list');

button.forEach((item) => item.parentNode.classList.remove('expand'));
button.forEach((menu) => menu.addEventListener('click', function () {
    let modal = document.querySelector('.menu-list');
    modal.classList.toggle('show');

    if (this.parentNode.classList != 'expand') {
        this.parentNode.classList.toggle('expand')
    }
    if (!modal.classList.contains('show')) {
        modal.style.height = modalheight + 'px';
    } else {
        modal.style.height = (this.parentNode.querySelector('ul').offsetHeight + 45) + 'px';
    }

    //back button

    menu.parentNode.querySelector('.back').addEventListener('click', function () {
        modal.style.height = 'auto';
        modal.classList.remove('show');
        menu.parentNode.classList.remove('expand')
    })
}))

const topmenu = document.querySelectorAll('[data-target]');
const panels = document.querySelectorAll('.wider > div:not(.main-menu)');

topmenu.forEach(item => {
    item.addEventListener('click', function (e) {
        e.preventDefault();

        const targetId = this.dataset.target;
        const targetPanel = document.getElementById(targetId);

        const isActive = targetPanel.classList.contains('active');

        // سكّر الكل
        panels.forEach(panel => panel.classList.remove('active'));

        // إذا ما كان مفتوح → افتحه
        if (!isActive) {
            targetPanel.classList.add('active');
        }
    });
});

