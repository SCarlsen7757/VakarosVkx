// Sidebar mobile toggle — CSS handles responsive collapse via media queries
(function () {
    function init() {
        const layout = document.querySelector('.app-layout');
        if (!layout) return;

        document.addEventListener('click', function (e) {
            if (e.target.closest('.sidebar-toggle')) {
                layout.classList.toggle('sidebar-open');
                return;
            }
            if (e.target.closest('.sidebar-backdrop')) {
                layout.classList.remove('sidebar-open');
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
