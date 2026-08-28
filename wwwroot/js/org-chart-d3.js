// Thin wrapper around the d3-org-chart library (MIT licensed,
// https://github.com/bumbeishvili/org-chart) for
// Components/Pages/Org/OrgChart.razor and OrganizationAdmin.razor's "ผังองค์กร
// (ภาพ)" tab. Blazor passes a flat JSON array of {id, parentId, orgId,
// orgName, employeeId, personName, title, photoUrl, initials, isVacant}
// records; the library owns layout, pan/zoom, and expand/collapse (via its
// own auto-rendered expand button, a separate DOM element from our custom
// card content below) — we only supply the per-node HTML template
// (nodeContent) plus click-through navigation on the header (-> org detail)
// and the person area (-> employee detail).
//
// Click-through is deliberately NOT wired through the library's own
// onNodeClick (that reports "a node was clicked", not which part of our
// custom HTML) — instead we delegate clicks on the container itself,
// looking for data-org-id/data-employee-id, and stopPropagation() so the
// library's own node-level click handling (default: a no-op) never fires
// for these. The library's expand button lives outside our nodeContent
// entirely, so it's unaffected either way.
window.OrgChartD3 = {
    chart: null,
    dotNetRef: null,
    delegatedContainerId: null,
    fullscreenListener: null,

    render: function (containerId, nodesJson, dotNetRef) {
        var nodes = JSON.parse(nodesJson);
        var container = document.getElementById(containerId);
        if (!container) return;

        this.dotNetRef = dotNetRef || null;

        var levelColors = ['#1e2a44', '#2f89b0', '#7c8f3c', '#5b5b7c'];

        if (!this.chart) {
            this.chart = new d3.OrgChart();
        }

        this.chart
            .container('#' + containerId)
            .data(nodes)
            .nodeWidth(function () { return 170; })
            .nodeHeight(function () { return 92; })
            .nodeContent(function (d) {
                var p = d.data;
                var headerColor = levelColors[d.depth % levelColors.length];

                var avatarHtml;
                if (p.photoUrl) {
                    avatarHtml = '<img class="org-chart-card-avatar" src="' + p.photoUrl + '" ' +
                        'alt="' + escapeHtml(p.personName || '') + '" title="' + escapeHtml(p.personName || '') + '" />';
                } else if (p.isVacant) {
                    avatarHtml = '<div class="org-chart-card-avatar org-chart-avatar-vacant">?</div>';
                } else {
                    avatarHtml = '<div class="org-chart-card-avatar org-chart-avatar-initials">' + escapeHtml(p.initials || '?') + '</div>';
                }

                var nameHtml = p.isVacant
                    ? '<div class="org-chart-card-vacant-label">ตำแหน่งว่าง</div>'
                    : '<div class="org-chart-card-name">' + escapeHtml(p.personName || '') + '</div>';

                var headerAttrs = p.orgId ? ' class="org-chart-card-header org-chart-clickable" data-org-id="' + p.orgId + '"' : ' class="org-chart-card-header"';
                var bodyAttrs = (!p.isVacant && p.employeeId) ? ' class="org-chart-card-body org-chart-clickable" data-employee-id="' + p.employeeId + '"' : ' class="org-chart-card-body"';

                return '' +
                    '<div class="org-chart-card' + (p.isVacant ? ' org-chart-vacant' : '') + '" style="width:170px;">' +
                    '  <div' + headerAttrs + ' style="background:' + headerColor + ';">' + escapeHtml(p.orgName || '') + '</div>' +
                    '  <div' + bodyAttrs + '>' +
                    avatarHtml +
                    '    <div class="org-chart-card-info">' +
                    nameHtml +
                    '      <div class="org-chart-card-title">' + escapeHtml(p.title || '') + '</div>' +
                    '    </div>' +
                    '  </div>' +
                    '</div>';
            })
            .render();

        this.setupClickDelegation(containerId);

        function escapeHtml(s) {
            return String(s).replace(/[&<>"']/g, function (c) {
                return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
            });
        }
    },

    // Re-registering render() (e.g. switching the root org in the
    // OrganizationAdmin.razor tab) tears down and rebuilds the chart's own
    // DOM, but the delegated listener is on the stable outer container, so
    // it only needs to be attached once per container.
    setupClickDelegation: function (containerId) {
        if (this.delegatedContainerId === containerId) return;
        this.delegatedContainerId = containerId;

        var container = document.getElementById(containerId);
        var self = this;
        container.addEventListener('click', function (event) {
            var empEl = event.target.closest('[data-employee-id]');
            if (empEl) {
                event.stopPropagation();
                if (self.dotNetRef) self.dotNetRef.invokeMethodAsync('OnEmployeeNodeClicked', parseInt(empEl.getAttribute('data-employee-id'), 10));
                return;
            }
            var orgEl = event.target.closest('[data-org-id]');
            if (orgEl) {
                event.stopPropagation();
                if (self.dotNetRef) self.dotNetRef.invokeMethodAsync('OnOrgNodeClicked', parseInt(orgEl.getAttribute('data-org-id'), 10));
            }
        });
    },

    expandAll: function () {
        if (!this.chart) return;
        this.chart.expandAll();
        this.chart.fit();
    },

    collapseAll: function () {
        if (!this.chart) return;
        this.chart.collapseAll();
        this.chart.fit();
    },

    // Browser Fullscreen API on the canvas-wrap div itself (its parent, one
    // level up from the #containerId the chart renders into) — fullscreening
    // just the chart area, not the whole <body>, so the page underneath is
    // untouched when the user exits. d3-org-chart sizes its SVG off the
    // container's own dimensions at render time, so after the fullscreen
    // transition finishes (fullscreenchange, not immediately — the layout
    // isn't settled yet at the moment requestFullscreen()'s promise resolves)
    // we re-render + fit() so the chart actually fills the new, much larger
    // area instead of staying pinned at its old 70vh size in the corner.
    toggleFullscreen: function (containerId) {
        var container = document.getElementById(containerId);
        if (!container) return;
        var wrap = container.closest('.org-chart-canvas-wrap') || container;

        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            wrap.requestFullscreen();
        }
    },

    isFullscreen: function () {
        return !!document.fullscreenElement;
    },

    // `document` outlives every page (SPA navigation never reloads it), so
    // registering a plain addEventListener here on every page visit would
    // stack up one stale listener per past visit, each holding a dotNetRef
    // into an already-disposed Blazor component — remove the previous
    // listener first so there's ever only one, bound to the current page.
    onFullscreenChange: function (dotNetRef) {
        if (this.fullscreenListener) {
            document.removeEventListener('fullscreenchange', this.fullscreenListener);
        }

        this.fullscreenListener = function () {
            var isFs = !!document.fullscreenElement;
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnFullscreenChanged', isFs);
            // Let the browser finish laying out the now-fullscreened (or
            // restored) element before asking d3-org-chart to refit — doing
            // it synchronously measures the pre-transition size.
            setTimeout(function () {
                if (window.OrgChartD3.chart) window.OrgChartD3.chart.fit();
            }, 150);
        };
        document.addEventListener('fullscreenchange', this.fullscreenListener);
    }
};
