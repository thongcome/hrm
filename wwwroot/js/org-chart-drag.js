// Free-form, non-persisted drag for org-chart position cards
// (Components/Pages/Org/OrgChart.razor). Purely visual rearrangement for
// readability — nothing here writes back to the server or to
// localStorage, so a page reload always resets cards to their computed
// layout. Each SVG connector <line> carries data-from/data-to attributes
// naming the DOM id of the card it's anchored to (see the Razor markup —
// only the first position-card of each org unit is wired to a line); on
// every drag frame we look up which lines reference the card being
// dragged and move that line's endpoint to the card's live position, so
// the connector visibly follows the card instead of staying pinned to
// where the card started.
window.OrgChartDrag = {
    init: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container || container.dataset.dragInit === '1') return;
        container.dataset.dragInit = '1';

        const svg = container.querySelector('svg.org-chart-svg');
        const linesByCardId = {};
        if (svg) {
            svg.querySelectorAll('line[data-from], line[data-to]').forEach(function (line) {
                const from = line.getAttribute('data-from');
                const to = line.getAttribute('data-to');
                if (from) { (linesByCardId[from] = linesByCardId[from] || []).push({ el: line, end: 'from' }); }
                if (to) { (linesByCardId[to] = linesByCardId[to] || []).push({ el: line, end: 'to' }); }
            });
        }

        function updateLinesFor(card) {
            const refs = linesByCardId[card.id];
            if (!refs) return;
            const x = parseFloat(card.style.left) || 0;
            const y = parseFloat(card.style.top) || 0;
            const centerX = x + card.offsetWidth / 2;
            refs.forEach(function (ref) {
                if (ref.end === 'from') {
                    // Line originates at the bottom-center of this card
                    // (this card is a parent org's representative).
                    ref.el.setAttribute('x1', centerX);
                    ref.el.setAttribute('y1', y + card.offsetHeight);
                } else {
                    // Line terminates at the top-center of this card
                    // (this card is a child org's representative).
                    ref.el.setAttribute('x2', centerX);
                    ref.el.setAttribute('y2', y);
                }
            });
        }

        let dragEl = null;
        let offsetX = 0;
        let offsetY = 0;

        container.querySelectorAll('.org-chart-card').forEach(function (card) {
            card.style.cursor = 'grab';
            card.style.touchAction = 'none';

            card.addEventListener('pointerdown', function (e) {
                dragEl = card;
                card.setPointerCapture(e.pointerId);
                card.style.cursor = 'grabbing';
                card.style.zIndex = 100;
                const rect = card.getBoundingClientRect();
                offsetX = e.clientX - rect.left;
                offsetY = e.clientY - rect.top;
                e.preventDefault();
            });

            card.addEventListener('pointermove', function (e) {
                if (dragEl !== card) return;
                const containerRect = container.getBoundingClientRect();
                const x = e.clientX - containerRect.left + container.scrollLeft - offsetX;
                const y = e.clientY - containerRect.top + container.scrollTop - offsetY;
                card.style.left = Math.max(0, x) + 'px';
                card.style.top = Math.max(0, y) + 'px';
                updateLinesFor(card);
            });

            function release(e) {
                if (dragEl !== card) return;
                card.style.cursor = 'grab';
                card.style.zIndex = '';
                dragEl = null;
            }
            card.addEventListener('pointerup', release);
            card.addEventListener('pointercancel', release);
        });
    }
};
