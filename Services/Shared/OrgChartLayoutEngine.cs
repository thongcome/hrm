namespace HRM.Services.Shared;

// Pure, static tree-layout algorithm for the draggable org chart canvas
// (Components/Pages/Org/OrgChart.razor). One "cluster" = one org unit's
// row of position-slot cards (1 card per Pos_PositionSlot, including
// vacant ones). Uses the standard subtree-width layout: a node's width is
// the larger of its own card row width or the combined width of its
// children's subtrees, so no cluster ever overlaps another. No DB/UI
// concerns here on purpose — pure geometry, independent of Blazor
// rendering, so it can be reasoned about (and unit-tested) on its own.
//
// Connector lines are anchored to each cluster's FIRST card (index 0 —
// the "representative" card for that org unit), not an abstract cluster
// midpoint. The page's JS drag handler (wwwroot/js/org-chart-drag.js)
// looks up which lines reference a card's DOM id and moves that line's
// endpoint live as the card is dragged, so the connector visibly follows
// its card instead of staying pinned to the card's original spot. Only
// card 0 of each org carries lines; sibling position-cards within the
// same org (index > 0) aren't individually wired to a parent/child line —
// they're just grouped by initial proximity.
public static class OrgChartLayoutEngine
{
    public const int CardWidth = 150;
    public const int CardHeight = 118;
    public const int CardGap = 12;      // gap between cards within one cluster
    public const int ClusterGap = 40;   // gap between sibling clusters
    public const int RowHeight = 190;   // vertical distance between depth levels

    public class ClusterNode
    {
        public required string OrgCode { get; init; }
        public required int CardCount { get; init; } // always >= 1 (a vacant placeholder card if the org has no position slots at all)
        public List<ClusterNode> Children { get; } = new();

        internal double SubtreeWidth { get; set; }
        internal double ClusterTopY { get; set; }
        internal double Card0CenterX { get; set; }
    }

    public record CardPosition(string OrgCode, int IndexInCluster, double X, double Y);
    public record ConnectorLine(string FromOrgCode, string ToOrgCode, double X1, double Y1, double X2, double Y2);
    public record LayoutResult(List<CardPosition> Cards, List<ConnectorLine> Lines, double TotalWidth, double TotalHeight);

    public static LayoutResult Layout(List<ClusterNode> roots)
    {
        var cards = new List<CardPosition>();
        var lines = new List<ConnectorLine>();

        double cursorX = 0;
        foreach (var root in roots)
        {
            ComputeWidth(root);
            cursorX = PlaceNode(root, cursorX, depth: 0, cards, lines, parent: null) + ClusterGap;
        }

        var totalWidth = roots.Count == 0 ? 0 : cursorX - ClusterGap;
        var maxDepth = roots.Count == 0 ? 0 : MaxDepth(roots);
        var totalHeight = (maxDepth + 1) * RowHeight;

        return new LayoutResult(cards, lines, totalWidth, totalHeight);
    }

    private static int MaxDepth(List<ClusterNode> nodes, int depth = 0) =>
        nodes.Count == 0 ? depth : nodes.Max(n => MaxDepth(n.Children, depth + 1));

    private static double ClusterOwnWidth(ClusterNode node) =>
        node.CardCount * CardWidth + (node.CardCount - 1) * CardGap;

    private static void ComputeWidth(ClusterNode node)
    {
        foreach (var child in node.Children) ComputeWidth(child);

        var ownWidth = ClusterOwnWidth(node);
        var childrenWidth = node.Children.Count == 0
            ? 0
            : node.Children.Sum(c => c.SubtreeWidth) + (node.Children.Count - 1) * ClusterGap;

        node.SubtreeWidth = Math.Max(ownWidth, childrenWidth);
    }

    private static double PlaceNode(ClusterNode node, double leftX, int depth,
        List<CardPosition> cards, List<ConnectorLine> lines, ClusterNode? parent)
    {
        var centerX = leftX + node.SubtreeWidth / 2;
        var topY = depth * RowHeight;
        node.ClusterTopY = topY;

        var ownWidth = ClusterOwnWidth(node);
        var firstCardX = centerX - ownWidth / 2;
        node.Card0CenterX = firstCardX + CardWidth / 2.0;
        for (var i = 0; i < node.CardCount; i++)
        {
            cards.Add(new CardPosition(node.OrgCode, i, firstCardX + i * (CardWidth + CardGap), topY));
        }

        if (parent is not null)
        {
            lines.Add(new ConnectorLine(
                parent.OrgCode, node.OrgCode,
                parent.Card0CenterX, parent.ClusterTopY + CardHeight,
                node.Card0CenterX, topY));
        }

        var childrenWidth = node.Children.Count == 0
            ? 0
            : node.Children.Sum(c => c.SubtreeWidth) + (node.Children.Count - 1) * ClusterGap;
        var childLeft = leftX + (node.SubtreeWidth - childrenWidth) / 2;

        foreach (var child in node.Children)
        {
            childLeft = PlaceNode(child, childLeft, depth + 1, cards, lines, node) + ClusterGap;
        }

        return leftX + node.SubtreeWidth;
    }
}
