// Sidebar toggle — works in static SSR (no Blazor interactivity required)
(function () {
    function init() {
        const layout = document.querySelector('.app-layout');
        if (!layout) return;

        // Collapse by default on mobile
        if (window.innerWidth < 1024) {
            layout.classList.add('sidebar-collapsed');
            layout.classList.remove('sidebar-open');
        }

        document.addEventListener('click', function (e) {
            const toggle = e.target.closest('.sidebar-toggle');
            if (toggle) {
                layout.classList.toggle('sidebar-open');
                layout.classList.toggle('sidebar-collapsed');
                return;
            }

            const backdrop = e.target.closest('.sidebar-backdrop');
            if (backdrop) {
                layout.classList.remove('sidebar-open');
                layout.classList.add('sidebar-collapsed');
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
