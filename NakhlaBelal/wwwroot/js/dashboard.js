// dashboard.js - Complete Dashboard Functionality

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('Dashboard initialized');

    // Initialize mobile navigation
    initMobileNavigation();

    // Initialize user profile dropdown
    initUserProfileDropdown();

    // Initialize search
    initSearch();

    // Initialize button animations
    initButtonAnimations();

    // Initialize filter dropdowns
    initFilterDropdowns();

    // Initialize modals
    initModals();

    // Initialize mobile enhancements
    initMobileEnhancements(); // Add this line
});

// ===== MOBILE DETECTION AND ENHANCEMENTS =====
function initMobileEnhancements() {
    const mobileScrollHint = document.querySelector('.mobile-scroll-hint');

    // Check if mobile
    function isMobile() {
        return window.innerWidth <= 768;
    }

    // Show/hide mobile hint
    if (mobileScrollHint) {
        if (isMobile()) {
            mobileScrollHint.style.display = 'block';
        }

        // Update on resize
        window.addEventListener('resize', function () {
            mobileScrollHint.style.display = isMobile() ? 'block' : 'none';
        });
    }

    // Improve touch scrolling for tables
    const tableContainers = document.querySelectorAll('.data-table-container');
    tableContainers.forEach(container => {
        let startX;
        let scrollLeft;

        container.addEventListener('touchstart', function (e) {
            startX = e.touches[0].pageX - container.offsetLeft;
            scrollLeft = container.scrollLeft;
        });

        container.addEventListener('touchmove', function (e) {
            if (!startX) return;
            const x = e.touches[0].pageX - container.offsetLeft;
            const walk = (x - startX) * 2; // Scroll multiplier
            container.scrollLeft = scrollLeft - walk;
        });
    });

    // Prevent accidental row clicks on mobile
    if (isMobile()) {
        document.addEventListener('click', function (e) {
            if (e.target.tagName === 'INPUT' && e.target.type === 'checkbox') {
                e.stopPropagation();
            }
        });
    }
}

// ===== USER PROFILE DROPDOWN =====
function initUserProfileDropdown() {
    const userProfile = document.getElementById('userProfile');
    const userDropdown = userProfile ? document.getElementById('userDropdown') : null;

    if (!userProfile || !userDropdown) return;

    // Toggle dropdown on click
    userProfile.addEventListener('click', function (e) {
        e.stopPropagation();
        userDropdown.classList.toggle('show');
        userDropdown.style.display = userDropdown.classList.contains('show') ? 'block' : 'none';
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function (e) {
        if (!userProfile.contains(e.target)) {
            userDropdown.classList.remove('show');
            userDropdown.style.display = 'none';
        }
    });

    // Close dropdown with ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            userDropdown.classList.remove('show');
            userDropdown.style.display = 'none';
        }
    });
}

// ===== MOBILE NAVIGATION =====
function initMobileNavigation() {
    const sideNav = document.getElementById('sideNav');
    const navToggle = document.getElementById('navToggle');
    const navClose = document.getElementById('navClose');
    const mobileOverlay = document.getElementById('mobileOverlay');

    if (navToggle) {
        navToggle.addEventListener('click', function () {
            sideNav.classList.add('active');
            mobileOverlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        });
    }

    if (navClose) {
        navClose.addEventListener('click', function () {
            sideNav.classList.remove('active');
            mobileOverlay.classList.remove('active');
            document.body.style.overflow = 'auto';
        });
    }

    if (mobileOverlay) {
        mobileOverlay.addEventListener('click', function () {
            sideNav.classList.remove('active');
            mobileOverlay.classList.remove('active');
            document.body.style.overflow = 'auto';
        });
    }
}

// ===== SEARCH FUNCTIONALITY =====
function initSearch() {
    const searchInput = document.getElementById('globalSearch');
    if (!searchInput) return;

    let searchTimeout;

    searchInput.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        const term = this.value.trim();

        if (term.length >= 2) {
            searchTimeout = setTimeout(() => {
                console.log('Searching for:', term);
                highlightSearchResults(term);
            }, 300);
        } else {
            clearSearch();
        }
    });
}

function highlightSearchResults(term) {
    const rows = document.querySelectorAll('table tbody tr');
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        if (text.includes(term.toLowerCase())) {
            row.style.backgroundColor = '#fffaf0';
        } else {
            row.style.backgroundColor = '';
        }
    });
}

function clearSearch() {
    const rows = document.querySelectorAll('table tbody tr');
    rows.forEach(row => {
        row.style.backgroundColor = '';
    });
}

// ===== BUTTON ANIMATIONS =====
function initButtonAnimations() {
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.filter-btn, .btn, .table-action, .row-action, .page-item');
        if (btn && !btn.classList.contains('disabled')) {
            btn.classList.add('click-animation');
            setTimeout(() => btn.classList.remove('click-animation'), 150);
        }
    });
}

// ===== FILTER DROPDOWNS =====
function initFilterDropdowns() {
    const filterBtns = document.querySelectorAll('.filter-group .filter-btn');

    filterBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            filterBtns.forEach(b => {
                if (b !== this) b.classList.remove('active');
            });
            this.classList.toggle('active');
        });
    });

    document.addEventListener('click', function () {
        filterBtns.forEach(btn => btn.classList.remove('active'));
    });
}

// ===== MODAL SYSTEM =====
function initModals() {
    // Close modals with ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeModal();
            closeConfirmModal();
        }
    });

    // Close modals when clicking outside
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('modal')) {
            closeModal();
            closeConfirmModal();
        }
    });

    console.log('Modal system initialized');
}

// ===== ORDER MODAL FUNCTIONS =====
function openOrderModal(orderId, event) {
    if (event) {
        event.stopPropagation();
        event.preventDefault();
    }

    console.log('Opening modal for order:', orderId);

    const modal = document.getElementById('universalModal');
    const modalContent = document.getElementById('modalContent');

    if (!modal || !modalContent) {
        console.error('Modal elements not found!');
        alert('Modal system error. Please refresh the page.');
        return;
    }

    // Set modal content
    modalContent.innerHTML = `
        <div class="modal-header">
            <h3>Order #${orderId}</h3>
            <button class="modal-close" onclick="closeModal()">
                <i class="fas fa-times"></i>
            </button>
        </div>
        <div class="modal-body">
            ${getOrderDetailsHTML(orderId)}
        </div>
        <div class="modal-footer">
            <button class="btn btn-secondary" onclick="closeModal()">Close</button>
            <button class="btn btn-primary" onclick="printOrder('${orderId}')">
                <i class="fas fa-print"></i> Print
            </button>
        </div>
    `;

    // Show modal
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    // Initialize tabs
    initModalTabs();
}

function getOrderDetailsHTML(orderId) {
    const orders = {
        '192541': {
            customer: 'Esther Howard',
            email: 'brodrigues@gmail.com',
            phone: '+1 (415) 555-2671',
            items: [
                { name: 'Stihl TS 800 cut-off machine incl.', desc: '5x diamond cutting discs', qty: 1, price: 1590.00 },
                { name: 'Gasoline generator EYG 7500I', desc: '(inverter)', qty: 1, price: 337.89 }
            ],
            total: 1927.89
        },
        '192540': {
            customer: 'David Miller',
            email: 'droidrigues@gmail.com',
            phone: '+1 (415) 555-1234',
            items: [
                { name: 'Generic Product', desc: 'Standard item', qty: 2, price: 795.00 }
            ],
            total: 1590.00
        }
    };

    const order = orders[orderId] || {
        customer: 'Customer',
        email: 'email@example.com',
        phone: '+1 (555) 123-4567',
        items: [
            { name: 'Sample Product', desc: 'Product description', qty: 1, price: 100.00 }
        ],
        total: 100.00
    };

    return `
        <div class="order-details">
            <div class="customer-info mb-3">
                <h4>${order.customer}</h4>
                <p><i class="fas fa-envelope"></i> ${order.email}</p>
                <p><i class="fas fa-phone"></i> ${order.phone}</p>
            </div>
            
            <div class="order-tabs mb-3">
                <button class="tab-btn active" data-tab="items">Order Items</button>
                <button class="tab-btn" data-tab="delivery">Delivery</button>
                <button class="tab-btn" data-tab="docs">Documents</button>
            </div>
            
            <div class="tab-content active" id="tab-items">
                <div class="order-items mb-3">
                    ${order.items.map(item => `
                        <div class="order-item mb-2 pb-2 border-bottom">
                            <div class="d-flex justify-content-between">
                                <div>
                                    <h5 class="mb-1">${item.name}</h5>
                                    <p class="text-muted mb-0">${item.desc}</p>
                                </div>
                                <div class="text-end">
                                    <p class="mb-1">${item.qty} × $${item.price.toFixed(2)}</p>
                                    <strong>$${(item.qty * item.price).toFixed(2)}</strong>
                                </div>
                            </div>
                        </div>
                    `).join('')}
                </div>
                
                <div class="order-total text-end">
                    <h4>Total: $${order.total.toFixed(2)}</h4>
                </div>
            </div>
            
            <div class="tab-content" id="tab-delivery" style="display: none;">
                <p>Delivery information for order #${orderId}</p>
                <p><strong>Address:</strong> 123 Main Street, San Francisco, CA 94107</p>
                <p><strong>Status:</strong> Shipped</p>
                <p><strong>Estimated Delivery:</strong> June 25, 2025</p>
            </div>
            
            <div class="tab-content" id="tab-docs" style="display: none;">
                <p>Documents for order #${orderId}</p>
                <ul class="list-unstyled">
                    <li><i class="fas fa-file-invoice"></i> Invoice #INV-${orderId}</li>
                    <li><i class="fas fa-receipt"></i> Receipt #REC-${orderId}</li>
                    <li><i class="fas fa-shipping-fast"></i> Shipping Label #SL-${orderId}</li>
                </ul>
            </div>
        </div>
    `;
}

function initModalTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', function () {
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => {
                content.classList.remove('active');
                content.style.display = 'none';
            });

            this.classList.add('active');
            const tabId = this.getAttribute('data-tab');
            const tabContent = document.getElementById(`tab-${tabId}`);
            if (tabContent) {
                tabContent.classList.add('active');
                tabContent.style.display = 'block';
            }
        });
    });
}

function closeModal() {
    const modal = document.getElementById('universalModal');
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = 'auto';
    }
}

// ===== ORDER ACTIONS =====
function exportOrder(orderId) {
    console.log('Exporting order:', orderId);
    alert(`Exporting order #${orderId}...`);
}

function duplicateOrder(orderId) {
    console.log('Duplicating order:', orderId);
    alert(`Duplicating order #${orderId}...`);
}

function printOrder(orderId) {
    console.log('Printing order:', orderId);
    window.print();
}

// ===== SELECTION MANAGEMENT =====
let selectedOrders = new Set();

function selectOrderRow(event, row) {
    if (event.target.type === 'checkbox' ||
        event.target.closest('.row-actions') ||
        event.target.closest('.product-info')) {
        return;
    }

    const checkbox = row.querySelector('.order-select');
    if (checkbox) {
        checkbox.checked = !checkbox.checked;
        updateSelection(checkbox);
    }
}

function updateSelection(checkbox) {
    const orderId = checkbox.value;

    if (checkbox.checked) {
        selectedOrders.add(orderId);
        checkbox.closest('tr').classList.add('selected');
    } else {
        selectedOrders.delete(orderId);
        checkbox.closest('tr').classList.remove('selected');
        const selectAll = document.getElementById('selectAll');
        if (selectAll) selectAll.checked = false;
    }

    updateSelectionUI();
}

function toggleSelectAll(selectAllCheckbox) {
    const checkboxes = document.querySelectorAll('.order-select');
    checkboxes.forEach(checkbox => {
        checkbox.checked = selectAllCheckbox.checked;
        updateSelection(checkbox);
    });
}

function updateSelectionUI() {
    const count = selectedOrders.size;
    const selectedCountEl = document.getElementById('selectedCount');
    const actionsBar = document.getElementById('selectionActions');

    if (selectedCountEl) {
        selectedCountEl.textContent = count;
    }

    if (actionsBar) {
        if (count > 0) {
            actionsBar.classList.add('active');
        } else {
            actionsBar.classList.remove('active');
        }
    }
}

function clearSelection() {
    selectedOrders.clear();
    document.querySelectorAll('.order-select').forEach(cb => {
        cb.checked = false;
        cb.closest('tr')?.classList.remove('selected');
    });
    const selectAll = document.getElementById('selectAll');
    if (selectAll) selectAll.checked = false;
    updateSelectionUI();
}

// ===== BULK ACTIONS =====
function exportSelected() {
    if (selectedOrders.size === 0) {
        alert('Please select at least one order');
        return;
    }
    alert(`Exporting ${selectedOrders.size} selected orders...`);
}

function deleteSelected() {
    if (selectedOrders.size === 0) {
        alert('Please select at least one order');
        return;
    }

    if (confirm(`Delete ${selectedOrders.size} selected orders?`)) {
        selectedOrders.forEach(id => {
            const row = document.querySelector(`tr[data-order-id="${id}"]`);
            if (row) row.remove();
        });
        clearSelection();
    }
}

// ===== CONFIRMATION MODAL =====
function showConfirmModal(title, message, onConfirm) {
    document.getElementById('confirmTitle').textContent = title;
    document.getElementById('confirmMessage').textContent = message;

    const confirmBtn = document.getElementById('confirmActionBtn');
    confirmBtn.onclick = function () {
        if (onConfirm) onConfirm();
        closeConfirmModal();
    };

    document.getElementById('confirmModal').style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

function closeConfirmModal() {
    document.getElementById('confirmModal').style.display = 'none';
    document.body.style.overflow = 'auto';
}

// ===== GLOBAL FUNCTIONS =====
window.openOrderModal = openOrderModal;
window.closeModal = closeModal;
window.selectOrderRow = selectOrderRow;
window.updateSelection = updateSelection;
window.toggleSelectAll = toggleSelectAll;
window.clearSelection = clearSelection;
window.exportSelected = exportSelected;
window.deleteSelected = deleteSelected;
window.exportOrder = exportOrder;
window.duplicateOrder = duplicateOrder;
window.printOrder = printOrder;