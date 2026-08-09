// Thin wrapper around the d3-org-chart library (MIT licensed,
// https://github.com/bumbeishvili/org-chart) for
// Components/Pages/Org/OrgChart.razor. Blazor passes a flat JSON array of
// {id, parentId, orgName, personName, title, photoUrl, initials, isVacant}
// records; the library owns layout, pan/zoom, and expand/collapse — we
// only supply the per-node HTML template (nodeContent) so each box keeps
// the same look (colored header by depth + avatar + name + title) that
// was already in use before this rewrite.
window.OrgChartD3 = {
    chart: null,

    render: function (containerId, nodesJson) {
        var nodes = JSON.parse(nodesJson);
        var container = document.getElementById(containerId);
        if (!container) return;

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

                return '' +
                    '<div class="org-chart-card' + (p.isVacant ? ' org-chart-vacant' : '') + '" style="width:170px;">' +
                    '  <div class="org-chart-card-header" style="background:' + headerColor + ';">' + escapeHtml(p.orgName || '') + '</div>' +
                    '  <div class="org-chart-card-body">' +
                    avatarHtml +
                    '    <div class="org-chart-card-info">' +
                    nameHtml +
                    '      <div class="org-chart-card-title">' + escapeHtml(p.title || '') + '</div>' +
                    '    </div>' +
                    '  </div>' +
                    '</div>';
            })
            .render();

        function escapeHtml(s) {
            return String(s).replace(/[&<>"']/g, function (c) {
                return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
            });
        }
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
    }
};
